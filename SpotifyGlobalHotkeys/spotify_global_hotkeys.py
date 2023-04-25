'''
Build Command:
pyinstaller --onefile --noconsole --add-data "icon.ico;." --icon=icon.ico -n "SpotifyGlobalHotkeys" spotify_global_hotkeys.py
'''
import os
import sys
import json
import time
import psutil
import pystray
import requests
import keyboard
import threading
import json.decoder
import winreg as reg
import tkinter as tk
from spotipy.oauth2 import SpotifyOAuth
from PIL import Image
from tkinter import ttk
from ttkthemes import ThemedTk

class SpotifyGlobalHotkeysApp(object):
    def __init__(self):
        def resource_path(relative_path):
            if getattr(sys, 'frozen', False):
                # running as an application
                base_path = sys._MEIPASS
            else:
                # running as a standalone script
                base_path = os.path.dirname(os.path.abspath(__file__))
            return os.path.join(base_path, relative_path)
        
        self.app_folder = os.path.join(os.environ['APPDATA'], '.spotify_global_hotkeys')
        os.makedirs(self.app_folder, exist_ok=True)
        self.config_path = os.path.join(self.app_folder, 'config.json')
        self.icon_path = resource_path('icon.ico')
        image = Image.open(self.icon_path)
        
        self.app = pystray.Icon('name', image, 'Spotify Global Hotkeys', menu=pystray.Menu(self.SettingsAction(), self.QuitAction()))

        # Spotify credentials
        self.username = None
        self.client_id = None
        self.client_secret = None
        self.redirect_uri = None
        self.device_id = None
        self.scope = 'user-modify-playback-state,user-read-playback-state'

        # Global hotkeys
        self.hotkeys = {
            'play/pause': None,
            'prev_track': None,
            'next_track': None,
            'volume_up': None,
            'volume_down': None
        }
        self.play_pause_hotkey = None
        self.prev_track_hotkey = None
        self.next_track_hotkey = None
        self.volume_up_hotkey = None
        self.volume_down_hotkey = None
        
        self.startup_var = None
        self.minimize_var = None

        # Spotify access token
        self.token = None
        
        # Start the loop to monitor system wake up events
        wakeup_thread = threading.Thread(target=self.MonitorWakeup)
        wakeup_thread.daemon = True
        wakeup_thread.start()
        
        self.refresh_thread_running = False
                
    # --------------------------------------------------------------------------------------- #
    '''
    MISC
    '''
    def MonitorWakeup(self):
        last_boot_time = None
        while True:
            current_boot_time = psutil.boot_time()
            if last_boot_time is not None and current_boot_time > last_boot_time:
                print("System woke up from sleep")
                self.SetHotkeys() # Reset hotkeys
                self.CreateToken() # Recreate token
            last_boot_time = current_boot_time
            time.sleep(10)
            
    def UpdateStartupRegistry(self):
        key_path = r'Software\Microsoft\Windows\CurrentVersion\Run'
        app_name = 'SpotifyGlobalHotkeys'
        exe_path = os.path.realpath(sys.argv[0])
        key = reg.OpenKey(reg.HKEY_CURRENT_USER, key_path, 0, reg.KEY_ALL_ACCESS)
        
        try:
            registry_value, _ = reg.QueryValueEx(key, app_name)
            if registry_value != exe_path:
                reg.SetValueEx(key, app_name, 0, reg.REG_SZ, exe_path)
        except FileNotFoundError:
            # Key not found, do nothing
            pass

        reg.CloseKey(key)

    def SetStartup(self, enabled):
        key = reg.HKEY_CURRENT_USER
        sub_key = r'Software\Microsoft\Windows\CurrentVersion\Run'
        app_name = 'SpotifyGlobalHotkeys'

        with reg.OpenKey(key, sub_key, 0, reg.KEY_ALL_ACCESS) as reg_key:
            if enabled:
                exe_path = os.path.realpath(sys.argv[0])
                reg.SetValueEx(reg_key, app_name, 0, reg.REG_SZ, exe_path)
            else:
                try:
                    reg.DeleteValue(reg_key, app_name)
                except FileNotFoundError:
                    pass
                    
    def SetHotkeys(self):
        print('Setting hotkeys')
        keyboard.unhook_all()  # Kill the previous keyboard listener
        self.play_pause_hotkey = keyboard.add_hotkey(self.hotkeys['play/pause'], lambda: (self.PlayPause(), time.sleep(0.5)))
        self.prev_track_hotkey = keyboard.add_hotkey(self.hotkeys['prev_track'], lambda: (self.PrevNext('previous'), time.sleep(0.5)))
        self.next_track_hotkey = keyboard.add_hotkey(self.hotkeys['next_track'], lambda: (self.PrevNext('next'), time.sleep(0.5)))
        self.volume_up_hotkey = keyboard.add_hotkey(self.hotkeys['volume_up'], lambda: (self.AdjustVolume(5), time.sleep(0)))
        self.volume_down_hotkey = keyboard.add_hotkey(self.hotkeys['volume_down'], lambda: (self.AdjustVolume(-5), time.sleep(0)))
        
    def SaveConfig(self):
        print('Saving config')
        # Add the hotkeys to the config dictionary
        config = {
            'startup': self.startup_var.get(),
            'minimize': self.minimize_var.get(),
            'username': self.username,
            'client_id': self.client_id,
            'client_secret': self.client_secret,
            'redirect_uri': self.redirect_uri,
            'device_id': self.device_id,
            'hotkeys': self.hotkeys
        }
        try:
            # Save the config to the file
            with open(self.config_path, 'w') as f:
                json.dump(config, f)
        except IOError as e:
            print(f'Error saving config: {e}')
        except Exception as e:
            print(f'Unexpected error while saving config: {e}')
        
    # --------------------------------------------------------------------------------------- #
    '''
    API
    '''
    def RefreshToken(self, auth_manager, expires_in):
        while True:
            time.sleep(expires_in - 60)  # Sleep until the token needs to be refreshed
            print('Refreshing token')
            token_data = auth_manager.refresh_access_token(auth_manager.get_cached_token()['refresh_token'])
            self.token = token_data['access_token']
            expires_in = token_data['expires_in']
            
    def CreateToken(self):
        print('Creating token')
        cache_file = os.path.join(self.app_folder, f'.cache-{self.username}')
        auth_manager = SpotifyOAuth(username=self.username,
                                    scope=self.scope,
                                    client_id=self.client_id,
                                    client_secret=self.client_secret,
                                    redirect_uri=self.redirect_uri,
                                    cache_path=cache_file)
        try:
            token_data = auth_manager.refresh_access_token(auth_manager.get_cached_token()['refresh_token'])
        except:
            token_data = auth_manager.get_access_token()

        self.token = token_data['access_token']
        expires_in = token_data['expires_in']

        # Start the loop to refresh the token before it expires, if not already running
        if not self.refresh_thread_running:
            print('Created refresh thread')
            refresh_thread = threading.Thread(target=self.RefreshToken, args=(auth_manager, expires_in))
            refresh_thread.daemon = True
            refresh_thread.start()
            self.refresh_thread_running = True
              
    def HandleConnectionError(self):
        # Connection error occurs when device is no longer active, so play music on specific device id
        headers = {'Authorization': 'Bearer ' + self.token}
        url = f'https://api.spotify.com/v1/me/player/play?device_id={self.device_id}'
        try:
            response = requests.put(url, headers=headers)
            response.raise_for_status()
            print('Playing music')
        except Exception as e:
            print(f'Error: {e}')
            
    def GetPlaybackState(self):
        headers = {'Authorization': 'Bearer ' + self.token}
        url = 'https://api.spotify.com/v1/me/player'
        try:
            response = requests.get(url, headers=headers)
            response.raise_for_status()
            playback_data = response.json()
            return playback_data['is_playing']
        except Exception as e:
            print(f'Error fetching playback state: {e}')
            self.HandleConnectionError()
            return None
 
    def PlayPause(self):
        is_playing = self.GetPlaybackState()
        if is_playing is None:
            return

        headers = {'Authorization': 'Bearer ' + self.token}
        if is_playing:
            # Pause the music
            url = f'https://api.spotify.com/v1/me/player/pause?device_id={self.device_id}'
        else:
            # Play the music
            url = f'https://api.spotify.com/v1/me/player/play?device_id={self.device_id}'
        try:
            response = requests.put(url, headers=headers)
            response.raise_for_status()
            if is_playing:
                print('Paused music')
            else:
                print('Playing music')
        except Exception as e:
            print(f'Error: {e}')
            
    def PrevNext(self, command):
        headers = {'Authorization': 'Bearer ' + self.token}
        url = f'https://api.spotify.com/v1/me/player/{command}?device_id={self.device_id}'
        try:
            response = requests.post(url, headers=headers)
            response.raise_for_status()
            if command == 'previous':
                print('Previous track')
            else:
                print('Next track')
        except Exception as e:
            print(f'Error: {e}')

    def AdjustVolume(self, amount):
        headers = {'Authorization': 'Bearer ' + self.token}
        url = f'https://api.spotify.com/v1/me/player/volume?volume_percent={self.GetCurrentVolume() + amount}'
        try:
            response = requests.put(url, headers=headers)
            response.raise_for_status()
            if amount > 0:
                print('Volume up')
            else:
                print('Volume down')
        except Exception as e:
            print(f'Error: {e}')

    def GetCurrentVolume(self):
        headers = {'Authorization': 'Bearer ' + self.token}
        url = 'https://api.spotify.com/v1/me/player'
        try:
            response = requests.get(url, headers=headers)
            response.raise_for_status()
            data = response.json()
            volume = data['device']['volume_percent']
            if (volume + 5) > 100:
                return 95
            elif (volume - 5) < 0:
                return 5
            return volume
        except Exception as e:
            print(f'Error: {e}')
            
    # --------------------------------------------------------------------------------------- #
    '''
    GUI
    '''
    def QuitAction(self):
        return pystray.MenuItem('Quit', self.Quit)

    def Quit(self, icon, item):
        self.app.stop()
        keyboard.unhook_all()  # Unhook all keys before exiting
        
        # Find the process by name
        for proc in psutil.process_iter(['name']):
            if proc.info['name'] == 'SpotifyGlobalHotkeys.exe':
                proc.kill()
    
    def SettingsAction(self):
        return pystray.MenuItem('Settings', self.OpenSettings)
            
    def OpenSettings(self, icon, item):
        self.SettingsWindow()

    def SettingsWindow(self):
        # Create the GUI
        root = ThemedTk(theme='breeze')
        root.title('Spotify Global Hotkeys')
        root.iconbitmap(self.icon_path)
        root.focus_force()
        
        def center_window(window):
            window.update_idletasks()
            width = window.winfo_width()
            height = window.winfo_height()
            x = (window.winfo_screenwidth() // 2) - (width // 2)
            y = (window.winfo_screenheight() // 2) - (height // 2)
            window.geometry(f'{width}x{height}+{x}+{y}')
            window.after(1, lambda: window.focus_force())
        
        def start_action():
            self.username = username_entry.get()
            self.client_id = client_id_entry.get()
            self.client_secret = client_secret_entry.get()
            self.redirect_uri = redirect_uri_entry.get()
            self.device_id = device_id_entry.get()
            
            self.hotkeys['play/pause'] = play_pause_entry.get()
            self.hotkeys['prev_track'] = prev_track_entry.get()
            self.hotkeys['next_track'] = next_track_entry.get()
            self.hotkeys['volume_up'] = volume_up_entry.get()
            self.hotkeys['volume_down'] = volume_down_entry.get()
            self.SetHotkeys()

            self.SaveConfig()
            self.SetStartup(self.startup_var.get())
            root.destroy()
            self.CreateToken()
            if not self.app.visible:
                self.run()

        # Create a frame with padding
        frame = ttk.Frame(root, padding=20)
        frame.grid(column=0, row=0)
        
        # Separator
        separator = ttk.Separator(frame, orient='horizontal')

        # Labels
        username_label = ttk.Label(frame, text='Spotify Username:')
        client_id_label = ttk.Label(frame, text='Client ID:')
        client_secret_label = ttk.Label(frame, text='Client Secret:')
        redirect_uri_label = ttk.Label(frame, text='Redirect URI:')
        device_id_label = ttk.Label(frame, text='Device ID:')
        play_pause_label = ttk.Label(frame, text='Play/Pause Hotkey:')
        prev_track_label = ttk.Label(frame, text='Previous Track Hotkey:')
        next_track_label = ttk.Label(frame, text='Next Track Hotkey:')
        volume_up_label = ttk.Label(frame, text='Volume Up Hotkey:')
        volume_down_label = ttk.Label(frame, text='Volume Down Hotkey:')

        # Entries
        username_entry = ttk.Entry(frame, width=35)
        client_id_entry = ttk.Entry(frame, width=35)
        client_secret_entry = ttk.Entry(frame, width=35)
        redirect_uri_entry = ttk.Entry(frame, width=35)
        device_id_entry = ttk.Entry(frame, width=35)
        play_pause_entry = ttk.Entry(frame, width=35)
        prev_track_entry = ttk.Entry(frame, width=35)
        next_track_entry = ttk.Entry(frame, width=35)
        volume_up_entry = ttk.Entry(frame, width=35)
        volume_down_entry = ttk.Entry(frame, width=35)

        # Button
        start_button = ttk.Button(frame, text='Start', command=start_action)
        
        # Checkboxes
        self.startup_var = tk.BooleanVar()
        self.minimize_var = tk.BooleanVar()

        # Autofill entries
        def autofill_entry(entry, value):
            entry.delete(0, tk.END)
            entry.insert(0, value)    
        
        entries = [username_entry, client_id_entry, client_secret_entry, redirect_uri_entry, device_id_entry]
        hotkey_entries = [play_pause_entry, prev_track_entry, next_track_entry, volume_up_entry, volume_down_entry]
        keys = ['username', 'client_id', 'client_secret', 'redirect_uri', 'device_id']  
        hotkey_keys = ['play/pause', 'prev_track', 'next_track', 'volume_up', 'volume_down']
        hotkey_defaults = ['ctrl+alt+shift+p', 'ctrl+alt+left', 'ctrl+alt+right', 'ctrl+alt+up', 'ctrl+alt+down']
        
        if os.path.exists(app.config_path):
            with open(app.config_path, 'r') as f:
                config = json.load(f)
                hotkeys = config['hotkeys']

                for entry, key in zip(entries, keys):
                    autofill_entry(entry, config.get(key, ''))
                for entry, key, default_value in zip(hotkey_entries, hotkey_keys, hotkey_defaults):
                    autofill_entry(entry, hotkeys.get(key, default_value))

                self.startup_var.set(config.get('startup', False))
                self.minimize_var.set(config.get('minimize', False))
        else:
            for entry, default_value in zip(hotkey_entries, hotkey_defaults):
                autofill_entry(entry, default_value)
                
            self.startup_var.set(False)
            self.minimize_var.set(False)
        
        # Checkboxes
        startup_checkbox = ttk.Checkbutton(frame, text='Start on Windows startup', variable=self.startup_var)
        minimize_checkbox = ttk.Checkbutton(frame, text='Start minimized', variable=self.minimize_var)
        
        # Grid layout
        username_label.grid(row=0, column=0, sticky='E')
        username_entry.grid(row=0, column=1)
        client_id_label.grid(row=1, column=0, sticky='E')
        client_id_entry.grid(row=1, column=1)
        client_secret_label.grid(row=2, column=0, sticky='E')
        client_secret_entry.grid(row=2, column=1)
        redirect_uri_label.grid(row=3, column=0, sticky='E')
        redirect_uri_entry.grid(row=3, column=1)
        device_id_label.grid(row=4, column=0, sticky='E')
        device_id_entry.grid(row=4, column=1)
        separator.grid(row=5, column=0, columnspan=3, sticky='ew', pady=10)
        play_pause_label.grid(row=6, column=0, sticky='E')
        play_pause_entry.grid(row=6, column=1)
        prev_track_label.grid(row=8, column=0, sticky='E')
        prev_track_entry.grid(row=8, column=1)
        next_track_label.grid(row=9, column=0, sticky='E')
        next_track_entry.grid(row=9, column=1)
        volume_up_label.grid(row=10, column=0, sticky='E')
        volume_up_entry.grid(row=10, column=1)
        volume_down_label.grid(row=11, column=0, sticky='E')
        volume_down_entry.grid(row=11, column=1)
        start_button.grid(row=12, column=1, pady=10)    
        startup_checkbox.grid(row=13, column=1, sticky='W')
        minimize_checkbox.grid(row=14, column=1, sticky='W')

        # Run the GUI
        center_window(root)
        root.mainloop()
        
    def run(self):
        self.UpdateStartupRegistry()
        self.app.run()
        
    # --------------------------------------------------------------------------------------- #
    
if __name__ == '__main__':
    def count_running_instances(process_name):
        count = 0
        for process in psutil.process_iter(['name']):
            if process.info['name'] == process_name:
                count += 1
        return count
    
    exe_name = 'SpotifyGlobalHotkeys.exe'
    running_instances = count_running_instances(exe_name)

    if running_instances > 2:
        print(f"{exe_name} is already running. Exiting...")
        sys.exit()
    else:
        print("Starting the application...")
        
    app = SpotifyGlobalHotkeysApp()

    if os.path.exists(app.config_path):
        with open(app.config_path, 'r') as f:
            config = json.load(f)
            hotkeys = config['hotkeys']
            
            for key, default_value in [('username', ''), ('client_id', ''), ('client_secret', ''), ('redirect_uri', ''), ('device_id', '')]:
                setattr(app, key, config.get(key, default_value))
            for key, default_value in [('play/pause', 'ctrl+alt+shift+p'), ('prev_track', 'ctrl+alt+left'), ('next_track', 'ctrl+alt+right'), ('volume_up', 'ctrl+alt+up'), ('volume_down', 'ctrl+alt+down')]:
                app.hotkeys[key] = hotkeys.get(key, default_value)
                
            app.SetHotkeys()
            
        if config.get('minimize', False):
            app.CreateToken()
            if not app.app.visible:
                app.run()
        else:
            app.SettingsWindow()
    else:
        app.SettingsWindow()