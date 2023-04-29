import os
import json
import psutil
import pystray
import keyboard
import threading
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
            
            self.app.hotkeys['play/pause'] = update_hotkey_entry(play_pause_entry, play_pause_modifiers, ctrl_play_pause_var, alt_play_pause_var, shift_play_pause_var)
            self.app.hotkeys['prev_track'] = update_hotkey_entry(prev_track_entry, prev_track_modifiers, ctrl_prev_track_var, alt_prev_track_var, shift_prev_track_var)
            self.app.hotkeys['next_track'] = update_hotkey_entry(next_track_entry, next_track_modifiers, ctrl_next_track_var, alt_next_track_var, shift_next_track_var)
            self.app.hotkeys['volume_up'] = update_hotkey_entry(volume_up_entry, volume_up_modifiers, ctrl_volume_up_var, alt_volume_up_var, shift_volume_up_var)
            self.app.hotkeys['volume_down'] = update_hotkey_entry(volume_down_entry, volume_down_modifiers, ctrl_volume_down_var, alt_volume_down_var, shift_volume_down_var)
            
        def update_hotkey_entry(entry, modifiers_frame, ctrl_var, alt_var, shift_var):
            modifiers = []
            if ctrl_var.get():
                modifiers.append('control')
            if alt_var.get():
                modifiers.append('alt')
            if shift_var.get():
                modifiers.append('shift')
            key = entry.get().strip().lower()
            hotkey = '+'.join(modifiers + [key])
            return hotkey
        
        def create_modifiers(frame, ctrl_var, alt_var, shift_var):
            ctrl_checkbox = ttk.Checkbutton(frame, text='Ctrl', variable=ctrl_var)
            alt_checkbox = ttk.Checkbutton(frame, text='Alt', variable=alt_var)
            shift_checkbox = ttk.Checkbutton(frame, text='Shift', variable=shift_var)
            return ctrl_checkbox, alt_checkbox, shift_checkbox
                    
        def center_window(window):
            window.update_idletasks()
            width = window.winfo_reqwidth()
            height = window.winfo_reqheight()
            x = (window.winfo_screenwidth() // 2) - (width // 2)
            y = (window.winfo_screenheight() // 2) - (height // 2)
            window.geometry(f'{width}x{height}+{x}+{y}')
            
        def save_action():
            set_input_fields()
            self.app.SaveConfig()
        
        def start_action():
            set_input_fields()
            if not self.app.CreateToken():
                return
            else:
                root.destroy()
                self.app.StopHotkeyListener()
                if not self.menu.visible:
                    self.run()
                else:
                    self.app.StartHotkeyListener()
            
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
        modifiers_label = ttk.Label(frame, text='Modifiers')
        key_label = ttk.Label(frame, text='Key')
        
        # Entries
        width = 35
        username_entry = ttk.Entry(frame, width=width)
        client_id_entry = ttk.Entry(frame, width=width)
        client_secret_entry = ttk.Entry(frame, width=width)
        redirect_uri_entry = ttk.Entry(frame, width=width)
        device_id_entry = ttk.Entry(frame, width=width)
        
        # Buttons
        button_frame = ttk.Frame(frame)
        save_button = ttk.Button(button_frame, text='Save', command=save_action)
        start_button = ttk.Button(button_frame, text='Start & Close', command=start_action)
        
        # Checkboxes
        self.app.startup_var = tk.BooleanVar()
        self.app.minimize_var = tk.BooleanVar()
        
        # Hotkey Area
        width = 8
        padding_x = 5
        padding_y = 2
        
        ctrl_play_pause_var = tk.BooleanVar()
        alt_play_pause_var = tk.BooleanVar()
        shift_play_pause_var = tk.BooleanVar()
        play_pause_modifiers = ttk.Frame(frame)
        play_pause_var = tk.StringVar()
        play_pause_entry = ttk.Entry(play_pause_modifiers, width=width, textvariable=play_pause_var)
        ctrl_play_pause_checkbox, alt_play_pause_checkbox, shift_play_pause_checkbox = \
            create_modifiers(play_pause_modifiers, ctrl_play_pause_var, alt_play_pause_var, shift_play_pause_var)
        ctrl_play_pause_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_play_pause_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_play_pause_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        play_pause_entry.grid(row=0, column=4, padx=padding_x, pady=padding_y)
        
        ctrl_prev_track_var = tk.BooleanVar()
        alt_prev_track_var = tk.BooleanVar()
        shift_prev_track_var = tk.BooleanVar()
        prev_track_modifiers = ttk.Frame(frame)
        prev_track_var = tk.StringVar()
        prev_track_entry = ttk.Entry(prev_track_modifiers, width=width, textvariable=prev_track_var)
        ctrl_prev_track_checkbox, alt_prev_track_checkbox, shift_prev_track_checkbox = \
            create_modifiers(prev_track_modifiers, ctrl_prev_track_var, alt_prev_track_var, shift_prev_track_var)
        ctrl_prev_track_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_prev_track_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_prev_track_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        prev_track_entry.grid(row=0, column=4, padx=padding_x, pady=padding_y)

        ctrl_next_track_var = tk.BooleanVar()
        alt_next_track_var = tk.BooleanVar()
        shift_next_track_var = tk.BooleanVar()
        next_track_modifiers = ttk.Frame(frame)
        next_track_var = tk.StringVar()
        next_track_entry = ttk.Entry(next_track_modifiers, width=width, textvariable=next_track_var)
        ctrl_next_track_checkbox, alt_next_track_checkbox, shift_next_track_checkbox = \
            create_modifiers(next_track_modifiers, ctrl_next_track_var, alt_next_track_var, shift_next_track_var)
        ctrl_next_track_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_next_track_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_next_track_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        next_track_entry.grid(row=0, column=4, padx=padding_x, pady=padding_y)

        ctrl_volume_up_var = tk.BooleanVar()
        alt_volume_up_var = tk.BooleanVar()
        shift_volume_up_var = tk.BooleanVar()
        volume_up_modifiers = ttk.Frame(frame)
        volume_up_var = tk.StringVar()
        volume_up_entry = ttk.Entry(volume_up_modifiers, width=width, textvariable=volume_up_var)
        ctrl_volume_up_checkbox, alt_volume_up_checkbox, shift_volume_up_checkbox = \
            create_modifiers(volume_up_modifiers, ctrl_volume_up_var, alt_volume_up_var, shift_volume_up_var)
        ctrl_volume_up_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_volume_up_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_volume_up_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        volume_up_entry.grid(row=0, column=4, padx=padding_x, pady=padding_y)

        ctrl_volume_down_var = tk.BooleanVar()
        alt_volume_down_var = tk.BooleanVar()
        shift_volume_down_var = tk.BooleanVar()
        volume_down_modifiers = ttk.Frame(frame)
        volume_down_var = tk.StringVar()
        volume_down_entry = ttk.Entry(volume_down_modifiers, width=width, textvariable=volume_down_var)
        ctrl_volume_down_checkbox, alt_volume_down_checkbox, shift_volume_down_checkbox = \
            create_modifiers(volume_down_modifiers, ctrl_volume_down_var, alt_volume_down_var, shift_volume_down_var)
        ctrl_volume_down_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_volume_down_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_volume_down_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        volume_down_entry.grid(row=0, column=4, padx=padding_x, pady=padding_y)

        # Autofill entries
        def autofill_entry(entry, value, hotkey=False):
            entry.delete(0, tk.END)
            if hotkey:
                modifiers, key = parse_hotkey_string(value)
                entry.insert(0, key)
                return modifiers
            else:
                entry.insert(0, value)

        def parse_hotkey_string(hotkey_str):
            modifiers = {'ctrl': False, 'alt': False, 'shift': False}
            key = ''

            if 'control' in hotkey_str:
                modifiers['ctrl'] = True
                hotkey_str = hotkey_str.replace('control', '')
            if 'alt' in hotkey_str:
                modifiers['alt'] = True
                hotkey_str = hotkey_str.replace('alt', '')
            if 'shift' in hotkey_str:
                modifiers['shift'] = True
                hotkey_str = hotkey_str.replace('shift', '')
            if '+' in hotkey_str:
                hotkey_str = hotkey_str.replace('+', '')

            key = hotkey_str.strip()
            return modifiers, key
  
        entries = [username_entry, client_id_entry, client_secret_entry, redirect_uri_entry, device_id_entry]
        hotkey_entries = [play_pause_entry, prev_track_entry, next_track_entry, volume_up_entry, volume_down_entry]
        keys = ['username', 'client_id', 'client_secret', 'redirect_uri', 'device_id']  
        hotkey_keys = ['play/pause', 'prev_track', 'next_track', 'volume_up', 'volume_down']
        hotkey_defaults = ['control+alt+shift+p', 'control+alt+shift+left', 'control+alt+shift+right', 'control+alt+shift+up', 'control+alt+shift+down']
        
        ctrl_vars = [ctrl_play_pause_var, ctrl_prev_track_var, ctrl_next_track_var, ctrl_volume_up_var, ctrl_volume_down_var]
        alt_vars = [alt_play_pause_var, alt_prev_track_var, alt_next_track_var, alt_volume_up_var, alt_volume_down_var]
        shift_vars = [shift_play_pause_var, shift_prev_track_var, shift_next_track_var, shift_volume_up_var, shift_volume_down_var]
        
        if os.path.exists(self.app.config_path):
            with open(self.app.config_path, 'r') as f:
                config = json.load(f)
                hotkeys = config['hotkeys']

                for entry, key in zip(entries, keys):
                    autofill_entry(entry, config.get(key, ''))
                
                for entry, key, default_value, ctrl_var, alt_var, shift_var in zip(hotkey_entries, hotkey_keys, hotkey_defaults, ctrl_vars, alt_vars, shift_vars):
                    modifiers = autofill_entry(entry, hotkeys.get(key, default_value), hotkey=True)
                    ctrl_var.set(modifiers['ctrl'])
                    alt_var.set(modifiers['alt'])
                    shift_var.set(modifiers['shift'])

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

        separator.grid(row=5, column=0, columnspan=3, sticky='EW', pady=10)
        
        labels_frame = ttk.Frame(frame)
        labels_frame.grid(row=6, column=1, pady=padding_y)

        modifier_label = ttk.Label(labels_frame, text='Modifiers')
        key_label = ttk.Label(labels_frame, text='Key')
        modifier_label.grid(row=0, column=1, padx=(70, 50))
        key_label.grid(row=0, column=3, padx=(40, 40))
        
        play_pause_label.grid(row=7, column=0, sticky='E')
        play_pause_modifiers.grid(row=7, column=1, sticky='W')
        prev_track_label.grid(row=8, column=0, sticky='E')
        prev_track_modifiers.grid(row=8, column=1, sticky='W')
        next_track_label.grid(row=9, column=0, sticky='E')
        next_track_modifiers.grid(row=9, column=1, sticky='W')
        volume_up_label.grid(row=10, column=0, sticky='E')
        volume_up_modifiers.grid(row=10, column=1, sticky='W')
        volume_down_label.grid(row=11, column=0, sticky='E')
        volume_down_modifiers.grid(row=11, column=1, sticky='W')
        
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