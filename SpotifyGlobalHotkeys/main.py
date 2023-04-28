'''
Build Command:
pyinstaller --onefile --noconsole --add-data "icon.ico;." --icon=icon.ico -n "SpotifyGlobalHotkeys" main.py
'''
import os
import sys
import json
import psutil
import threading
import json.decoder
import win32gui

from app import *
from view import *

if __name__ == '__main__':
    # If the user trys to open another instance, do not open
    def count_running_instances():
        count = 0
        for process in psutil.process_iter(['name']):
            if process.info['name'] == 'SpotifyGlobalHotkeys.exe':
                count += 1
        return count
    if count_running_instances() > 2:
        print('SpotifyGlobalHotkeys.exe is already running. Exiting...')
        sys.exit()
    
    print('Starting the application...')
    app = SpotifyGlobalHotkeysApp()
    gui = SpotifyGlobalHotkeysView(app)

    # Check if there is a config file
    if os.path.exists(app.config_path):
        with open(app.config_path, 'r') as f:
            config = json.load(f)
            hotkeys = config['hotkeys']
            
            for key, default_value in [('username', ''), ('client_id', ''), ('client_secret', ''), ('redirect_uri', ''), ('device_id', '')]:
                setattr(app, key, config.get(key, default_value))
            for key, default_value in [('play/pause', 'control+alt+shift+p'), ('prev_track', 'control+alt+shift+left'), ('next_track', 'control+alt+shift+right'), ('volume_up', 'control+alt+shift+up'), ('volume_down', 'control+alt+shift+down')]:
                app.hotkeys[key] = hotkeys.get(key, default_value)
        
        # If minimize is True, do not open the Settings window
        if config.get('minimize', False):
            app.CreateToken()
            if not gui.menu.visible:
                gui.run()
        else:
            gui.SettingsWindow()
    else:
        gui.SettingsWindow()
        
    # Start message pump to process Windows messages    
    def start_message_pump():
        win32gui.PumpMessages()
    message_pump_thread = threading.Thread(target=start_message_pump)
    message_pump_thread.daemon = True
    message_pump_thread.start()