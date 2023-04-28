import os
import json
import psutil
import pystray
import keyboard
import json.decoder
import tkinter as tk
from PIL import Image
from tkinter import ttk
from ttkthemes import ThemedTk

class SpotifyGlobalHotkeysView(object):
    def __init__(self, app):
        self.app = app
        self.icon_path = app.icon_path

        image = Image.open(self.icon_path)
        self.menu = pystray.Icon('name', image, 'Spotify Global Hotkeys', menu=pystray.Menu(self.SettingsAction(), self.QuitAction()))
        
    def QuitAction(self):
        return pystray.MenuItem('Quit', self.Quit)

    def Quit(self, icon, item):
        self.menu.stop()
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
        def set_input_fields():
            self.app.SetStartup(self.app.startup_var.get())
            self.app.username = username_entry.get()
            self.app.client_id = client_id_entry.get()
            self.app.client_secret = client_secret_entry.get()
            self.app.redirect_uri = redirect_uri_entry.get()
            self.app.device_id = device_id_entry.get()
            self.app.hotkeys['play/pause'] = play_pause_entry.get()
            self.app.hotkeys['prev_track'] = prev_track_entry.get()
            self.app.hotkeys['next_track'] = next_track_entry.get()
            self.app.hotkeys['volume_up'] = volume_up_entry.get()
            self.app.hotkeys['volume_down'] = volume_down_entry.get()
            
        def save_action():
            set_input_fields()
            self.app.SaveConfig()
        
        def start_action():
            set_input_fields()
            if not self.app.CreateToken():
                return
            else:
                root.destroy()
                self.app.RestartHotkeyListener()
                if not self.menu.visible:
                    self.run()
                    
        def center_window(window):
            window.update_idletasks()
            width = window.winfo_reqwidth()
            height = window.winfo_reqheight()
            x = (window.winfo_screenwidth() // 2) - (width // 2)
            y = (window.winfo_screenheight() // 2) - (height // 2)
            window.geometry(f'{width}x{height}+{x}+{y}')
            
        # Create the GUI
        root = ThemedTk(theme='breeze')
        root.withdraw()
        root.title('Spotify Global Hotkeys')
        root.iconbitmap(self.icon_path)

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
        
        # Buttons
        button_frame = ttk.Frame(frame)
        save_button = ttk.Button(button_frame, text='Save', command=save_action)
        start_button = ttk.Button(button_frame, text='Start & Close', command=start_action)
        
        # Checkboxes
        self.app.startup_var = tk.BooleanVar()
        self.app.minimize_var = tk.BooleanVar()

        # Autofill entries
        def autofill_entry(entry, value):
            entry.delete(0, tk.END)
            entry.insert(0, value)    
        
        entries = [username_entry, client_id_entry, client_secret_entry, redirect_uri_entry, device_id_entry]
        hotkey_entries = [play_pause_entry, prev_track_entry, next_track_entry, volume_up_entry, volume_down_entry]
        keys = ['username', 'client_id', 'client_secret', 'redirect_uri', 'device_id']  
        hotkey_keys = ['play/pause', 'prev_track', 'next_track', 'volume_up', 'volume_down']
        hotkey_defaults = ['<ctrl>+<alt>+<shift>+p', '<ctrl>+<alt>+<left>', '<ctrl>+<alt>+<right>', '<ctrl>+<alt>+<up>', '<ctrl>+<alt>+<down>']
        
        if os.path.exists(self.app.config_path):
            with open(self.app.config_path, 'r') as f:
                config = json.load(f)
                hotkeys = config['hotkeys']

                for entry, key in zip(entries, keys):
                    autofill_entry(entry, config.get(key, ''))
                for entry, key, default_value in zip(hotkey_entries, hotkey_keys, hotkey_defaults):
                    autofill_entry(entry, hotkeys.get(key, default_value))

                self.app.startup_var.set(config.get('startup', False))
                self.app.minimize_var.set(config.get('minimize', False))
        else:
            for entry, default_value in zip(hotkey_entries, hotkey_defaults):
                autofill_entry(entry, default_value)
                
            self.app.startup_var.set(False)
            self.app.minimize_var.set(False)
        
        # Checkboxes
        checkbox_frame = ttk.Frame(frame)
        startup_checkbox = ttk.Checkbutton(checkbox_frame, text='Start on Windows startup', variable=self.app.startup_var)
        minimize_checkbox = ttk.Checkbutton(checkbox_frame, text='Start minimized', variable=self.app.minimize_var)
        
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
        button_frame.grid(row=12, column=0, columnspan=2, pady=10)
        save_button.pack(side='left', padx=(0, 5))
        start_button.pack(side='left', padx=(5, 0))
        checkbox_frame.grid(row=13, column=0, columnspan=2, pady=10)
        startup_checkbox.pack(side='left', padx=(0, 5))
        minimize_checkbox.pack(side='left', padx=(5, 0))
        
        center_window(root)
        root.deiconify()
        root.update()
        root.focus_force()
        
        # Run the GUI
        root.mainloop()
    
    def run(self):
        self.app.UpdateStartupRegistry()
        self.app.StartHotkeyListener()
        self.menu.run()