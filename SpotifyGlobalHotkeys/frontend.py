import os
import json
import psutil
import pystray
import win32api
import win32con
import json.decoder
import tkinter as tk
from PIL import Image
from tkinter import ttk
from ttkthemes import ThemedTk
from global_hotkeys import keycodes

class Frontend(object):
    def __init__(self, app):
        self.app = app
        self.modified = False
        self.modified_cred = False
        self.icon_path = app.icon_path

        image = Image.open(self.icon_path)
        self.menu = pystray.Icon('name', image, 'Spotify Global Hotkeys', menu=pystray.Menu(self.SettingsAction(), self.QuitAction()))
        
    def QuitAction(self):
        return pystray.MenuItem('Quit', self.Quit)

    def Quit(self, icon, item):
        self.menu.stop()
        
        # Find the process by name
        for proc in psutil.process_iter(['name']):
            if proc.info['name'] == 'SpotifyGlobalHotkeys.exe':
                proc.kill()
    
    def SettingsAction(self):
        return pystray.MenuItem('Settings', self.OpenSettings)
      
    def OpenSettings(self, icon, item):
        self.SettingsWindow()

    def SettingsWindow(self):
        global KEY_OPTIONS
        
        def set_modified(event=None):
            self.modified = True
            save_button.config(state=tk.NORMAL)
            
        def set_modified_cred(event=None):
            self.modified_cred = True
            self.modified = True
            save_button.config(state=tk.NORMAL)
        
        def handle_keypress(virtual_keycode, entry):
            decimal_to_keyname = {int(hex_value): key for key, hex_value in keycodes.vk_key_names.items()}
            key_name = decimal_to_keyname.get(virtual_keycode)
            
            if key_name == 'backspace' or key_name == 'delete':
                entry.delete(0, tk.END)
            elif key_name in KEY_OPTIONS:
                entry.delete(0, tk.END)
                entry.insert(0, key_name)

        def listen_for_key_events(entry):
            while True:
                try:
                    if entry.focus_get() != entry:
                        break
                    for key_name, virtual_keycode in keycodes.vk_key_names.items():
                        if win32api.GetAsyncKeyState(virtual_keycode) & 0x8000:
                            handle_keypress(virtual_keycode, entry)
                    entry.update_idletasks()
                    entry.update()
                except:
                    break
            
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
        
        def set_input_fields():
            self.app.SetStartup(self.app.startup_var.get())
            self.app.client_id = client_id_entry.get()
            self.app.client_secret = client_secret_entry.get()
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
            ctrl_checkbox.config(command=set_modified)
            alt_checkbox.config(command=set_modified)
            shift_checkbox.config(command=set_modified)
            return ctrl_checkbox, alt_checkbox, shift_checkbox
                    
        def center_window(window):
            window.update_idletasks()
            width = window.winfo_reqwidth()
            height = window.winfo_reqheight()
            x = (window.winfo_screenwidth() // 2) - (width // 2)
            y = (window.winfo_screenheight() // 2) - (height // 2)
            window.geometry(f'{width}x{height}+{x}+{y}')
            window.after(1, lambda: window.focus_force())
            
        def save_action():
            set_input_fields()
            self.app.SaveConfig()
        
        def start_action():
            set_input_fields()
            if self.modified_cred and not self.app.CreateToken():
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
        root.focus_force()

        # Create a frame with padding
        frame = ttk.Frame(root, padding=20)
        frame.grid(column=0, row=0)
        
        # Separator
        separator = ttk.Separator(frame, orient='horizontal')

        # Labels
        client_id_label = ttk.Label(frame, text='Client ID:')
        client_secret_label = ttk.Label(frame, text='Client Secret:')
        device_id_label = ttk.Label(frame, text='Device ID:')
        play_pause_label = ttk.Label(frame, text='Play/Pause:')
        prev_track_label = ttk.Label(frame, text='Previous Track:')
        next_track_label = ttk.Label(frame, text='Next Track:')
        volume_up_label = ttk.Label(frame, text='Volume Up:')
        volume_down_label = ttk.Label(frame, text='Volume Down:')
        
        labels_frame = ttk.Frame(frame)
        modifier_label = ttk.Label(labels_frame, text='Modifiers')
        key_label = ttk.Label(labels_frame, text='Key')
        
        # Entries
        width = 40
        client_id_entry = ttk.Entry(frame, width=width)
        client_secret_entry = ttk.Entry(frame, width=width)
        device_id_entry = ttk.Entry(frame, width=width)
        
        # Buttons
        button_frame = ttk.Frame(frame)
        save_button = ttk.Button(button_frame, text='Save', command=save_action, state=tk.DISABLED)
        start_button = ttk.Button(button_frame, text='Start & Close', command=start_action)
        
        # Checkboxes
        self.app.startup_var = tk.BooleanVar()
        self.app.minimize_var = tk.BooleanVar()
        
        # Hotkey Area
        width = 12
        padding_x = 2
        padding_y = 2
        
        ctrl_play_pause_var = tk.BooleanVar()
        alt_play_pause_var = tk.BooleanVar()
        shift_play_pause_var = tk.BooleanVar()
        play_pause_modifiers = ttk.Frame(frame)
        play_pause_entry = ttk.Entry(play_pause_modifiers, width=width, justify='center')
        play_pause_entry.bind('<FocusIn>', lambda event: listen_for_key_events(play_pause_entry))
        ctrl_play_pause_checkbox, alt_play_pause_checkbox, shift_play_pause_checkbox = \
            create_modifiers(play_pause_modifiers, ctrl_play_pause_var, alt_play_pause_var, shift_play_pause_var)
        ctrl_play_pause_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_play_pause_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_play_pause_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        play_pause_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)
        
        ctrl_prev_track_var = tk.BooleanVar()
        alt_prev_track_var = tk.BooleanVar()
        shift_prev_track_var = tk.BooleanVar()
        prev_track_modifiers = ttk.Frame(frame)
        prev_track_entry = ttk.Entry(prev_track_modifiers, width=width, justify='center')
        prev_track_entry.bind('<FocusIn>', lambda event: listen_for_key_events(prev_track_entry))
        ctrl_prev_track_checkbox, alt_prev_track_checkbox, shift_prev_track_checkbox = \
            create_modifiers(prev_track_modifiers, ctrl_prev_track_var, alt_prev_track_var, shift_prev_track_var)
        ctrl_prev_track_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_prev_track_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_prev_track_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        prev_track_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_next_track_var = tk.BooleanVar()
        alt_next_track_var = tk.BooleanVar()
        shift_next_track_var = tk.BooleanVar()
        next_track_modifiers = ttk.Frame(frame)
        next_track_entry = ttk.Entry(next_track_modifiers, width=width, justify='center')
        next_track_entry.bind('<FocusIn>', lambda event: listen_for_key_events(next_track_entry))
        ctrl_next_track_checkbox, alt_next_track_checkbox, shift_next_track_checkbox = \
            create_modifiers(next_track_modifiers, ctrl_next_track_var, alt_next_track_var, shift_next_track_var)
        ctrl_next_track_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_next_track_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_next_track_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        next_track_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_volume_up_var = tk.BooleanVar()
        alt_volume_up_var = tk.BooleanVar()
        shift_volume_up_var = tk.BooleanVar()
        volume_up_modifiers = ttk.Frame(frame)
        volume_up_entry = ttk.Entry(volume_up_modifiers, width=width, justify='center')
        volume_up_entry.bind('<FocusIn>', lambda event: listen_for_key_events(volume_up_entry))
        ctrl_volume_up_checkbox, alt_volume_up_checkbox, shift_volume_up_checkbox = \
            create_modifiers(volume_up_modifiers, ctrl_volume_up_var, alt_volume_up_var, shift_volume_up_var)
        ctrl_volume_up_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_volume_up_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_volume_up_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        volume_up_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_volume_down_var = tk.BooleanVar()
        alt_volume_down_var = tk.BooleanVar()
        shift_volume_down_var = tk.BooleanVar()
        volume_down_modifiers = ttk.Frame(frame)
        volume_down_entry = ttk.Entry(volume_down_modifiers, width=width, justify='center')
        volume_down_entry.bind('<FocusIn>', lambda event: listen_for_key_events(volume_down_entry))
        ctrl_volume_down_checkbox, alt_volume_down_checkbox, shift_volume_down_checkbox = \
            create_modifiers(volume_down_modifiers, ctrl_volume_down_var, alt_volume_down_var, shift_volume_down_var)
        ctrl_volume_down_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_volume_down_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_volume_down_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        volume_down_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        # Autofill entries
        entries = [client_id_entry, client_secret_entry, device_id_entry]
        hotkey_entries = [play_pause_entry, prev_track_entry, next_track_entry, volume_up_entry, volume_down_entry]
        keys = ['client_id', 'client_secret', 'device_id']  
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
                
            for entry, key, default_value, ctrl_var, alt_var, shift_var in zip(hotkey_entries, hotkey_keys, hotkey_defaults, ctrl_vars, alt_vars, shift_vars):
                modifiers = autofill_entry(entry, default_value, hotkey=True)
                ctrl_var.set(modifiers['ctrl'])
                alt_var.set(modifiers['alt'])
                shift_var.set(modifiers['shift'])
                
            self.app.startup_var.set(False)
            self.app.minimize_var.set(False)
        
        # Checkboxes
        checkbox_frame = ttk.Frame(frame)
        startup_checkbox = ttk.Checkbutton(checkbox_frame, text='Start on Windows startup', variable=self.app.startup_var)
        minimize_checkbox = ttk.Checkbutton(checkbox_frame, text='Start minimized', variable=self.app.minimize_var)
        
        # Check if modified
        client_id_entry.bind('<KeyRelease>', set_modified_cred)
        client_secret_entry.bind('<KeyRelease>', set_modified_cred)
        device_id_entry.bind('<KeyRelease>', set_modified_cred)
        play_pause_entry.bind('<KeyRelease>', set_modified)
        prev_track_entry.bind('<KeyRelease>', set_modified)
        next_track_entry.bind('<KeyRelease>', set_modified)
        volume_up_entry.bind('<KeyRelease>', set_modified)
        volume_down_entry.bind('<KeyRelease>', set_modified)
        startup_checkbox.config(command=set_modified)
        minimize_checkbox.config(command=set_modified)
        
        # Grid layout
        client_id_label.grid(row=1, column=0, sticky='E')
        client_id_entry.grid(row=1, column=1)
        client_secret_label.grid(row=2, column=0, sticky='E')
        client_secret_entry.grid(row=2, column=1)
        device_id_label.grid(row=4, column=0, sticky='E')
        device_id_entry.grid(row=4, column=1)

        separator.grid(row=5, column=0, columnspan=3, sticky='EW', pady=10)
    
        labels_frame.grid(row=6, column=1, pady=padding_y)
        modifier_label.grid(row=0, column=1, padx=(10, 50))
        key_label.grid(row=0, column=3, padx=(50, 0))
        
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
       
        # Center window and focus
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
        
KEY_OPTIONS = [
    "a",
    "b",
    "c",
    "d",
    "e",
    "f",
    "g",
    "h",
    "i",
    "j",
    "k",
    "l",
    "m",
    "n",
    "o",
    "p",
    "q",
    "r",
    "s",
    "t",
    "u",
    "v",
    "w",
    "x",
    "y",
    "z",
    "0",
    "1",
    "2",
    "3",
    "4",
    "5",
    "6",
    "7",
    "8",
    "9",
    "f1",
    "f2",
    "f3",
    "f4",
    "f5",
    "f6",
    "f7",
    "f8",
    "f9",
    "f10",
    "f11",
    "f12",
    "f13",
    "f14",
    "f15",
    "f16",
    "f17",
    "f18",
    "f19",
    "f20",
    "f21",
    "f22",
    "f23",
    "f24",
    "=",
    ",",
    "-",
    ".",
    "/",
    "`",
    ";",
    "[",
    "\\",
    "]",
    "'",
    "`",
    "backspace",
    "tab",
    "clear",
    "enter",
    "pause",
    "caps_lock",
    "escape",
    "space",
    "page_up",
    "page_down",
    "end",
    "home",
    "left",
    "up",
    "right",
    "down",
    "print",
    "enter",
    "print_screen",
    "insert",
    "delete",
    "numpad_0",
    "numpad_1",
    "numpad_2",
    "numpad_3",
    "numpad_4",
    "numpad_5",
    "numpad_6",
    "numpad_7",
    "numpad_8",
    "numpad_9",
    "multiply_key",
    "add_key",
    "subtract_key",
    "decimal_key",
    "divide_key",
]