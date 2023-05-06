import os
import sys
import json
import psutil
import threading
import json.decoder
import win32gui
from backend import *
from frontend import *

def main():
    # If the user trys to open another instance, do not open
    def count_running_instances():
        count = 0
        for process in psutil.process_iter(['name']):
            if process.info['name'] == 'BeatBind.exe':
                count += 1
        return count
    if count_running_instances() > 2:
        print('BeatBind.exe is already running. Exiting...')
        sys.exit()
    
    print('Starting the application...')
    backend = Backend()
    frontend = Frontend(backend)

    # Check if there is a config file
    if os.path.exists(backend.config_path):
        with open(backend.config_path, 'r') as f:
            config = json.load(f)
            hotkeys = config['hotkeys']
            
            for key, default_value in [('client_id', ''), ('client_secret', ''), ('device_id', '')]:
                setattr(backend, key, config.get(key, default_value))
            for key, default_value in [('play/pause', 'control+alt+shift+p'), ('prev_track', 'control+alt+shift+left'), ('next_track', 'control+alt+shift+right'), ('volume_up', 'control+alt+shift+up'), ('volume_down', 'control+alt+shift+down')]:
                backend.hotkeys[key] = hotkeys.get(key, default_value)
        
        # If minimize is True, do not open the Settings window
        if config.get('minimize', False):
            backend.CheckTokenExpiry()
            if not frontend.menu.visible:
                frontend.run()
        else:
            frontend.SettingsWindow()
    else:
        frontend.SettingsWindow()
        
    # Start message pump to process Windows messages    
    def start_message_pump():
        win32gui.PumpMessages()
    message_pump_thread = threading.Thread(target=start_message_pump)
    message_pump_thread.daemon = True
    message_pump_thread.start()

if __name__ == '__main__':
    main()