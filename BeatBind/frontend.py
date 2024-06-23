import json
import json.decoder
import os
import tkinter as tk
import webbrowser
from tkinter import ttk

import psutil
import pystray
import win32api
from global_hotkeys import keycodes
from PIL import Image
from ttkthemes import ThemedTk


class Frontend(object):
    def __init__(self, app):
        self.app = app
        self.icon_path = app.icon_path
        self.modified_credentials = False

        image = Image.open(self.icon_path)
        self.menu = pystray.Icon(
            "name",
            image,
            "BeatBind",
            menu=pystray.Menu(self.SettingsAction(), self.QuitAction()),
        )

    def QuitAction(self):
        return pystray.MenuItem("Quit", self.Quit)

    def Quit(self):
        self.menu.stop()

        # Find the process by name
        for proc in psutil.process_iter(["name"]):
            if proc.info["name"] == "BeatBind.exe":
                proc.kill()

    def SettingsAction(self):
        return pystray.MenuItem("Settings", self.SettingsWindow)

    def SettingsWindow(self):

        def set_modified(event=None):
            save_button.config(state=tk.NORMAL)

        def set_modified_cred(event=None):
            self.modified_credentials = True
            save_button.config(state=tk.NORMAL)

        def handle_keypress(key_name, entry):
            if key_name == "backspace" or key_name == "delete":
                entry.delete(0, tk.END)
            else:
                entry.delete(0, tk.END)
                entry.insert(0, key_name)

        def listen_for_key_events(entry):
            while True:
                try:
                    if entry.focus_get() != entry:
                        break
                    for key_name, virtual_keycode in keycodes.vk_key_names.items():
                        if virtual_keycode == "window":
                            continue
                        if win32api.GetAsyncKeyState(virtual_keycode) & 0x8000:
                            handle_keypress(key_name, entry)
                    entry.update_idletasks()
                    entry.update()
                except Exception as e:
                    print(f"Error: {e}")
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
            modifiers = {"ctrl": False, "alt": False, "shift": False, "win": False}
            key = ""
            if "control" in hotkey_str:
                modifiers["ctrl"] = True
                hotkey_str = hotkey_str.replace("control", "")
            if "alt" in hotkey_str:
                modifiers["alt"] = True
                hotkey_str = hotkey_str.replace("alt", "")
            if "shift" in hotkey_str:
                modifiers["shift"] = True
                hotkey_str = hotkey_str.replace("shift", "")
            if "window" in hotkey_str:
                modifiers["win"] = True
                hotkey_str = hotkey_str.replace("window", "")
            if "+" in hotkey_str:
                hotkey_str = hotkey_str.replace("+", "")
            key = hotkey_str.strip()
            return modifiers, key

        def update_hotkey_entry(entry, ctrl_var, alt_var, shift_var, win_var):
            modifiers = []
            if ctrl_var.get():
                modifiers.append("control")
            if alt_var.get():
                modifiers.append("alt")
            if shift_var.get():
                modifiers.append("shift")
            if win_var.get():
                modifiers.append("window")
            key = entry.get().strip().lower()
            hotkey = "+".join(modifiers + [key])
            return hotkey

        def set_input_fields():
            self.app.SetStartup(self.app.startup_var.get())
            self.app.client_id = client_id_entry.get()
            self.app.client_secret = client_secret_entry.get()
            self.app.port = int(port_entry.get())
            self.app.device_id = device_id_entry.get()
            self.app.volume = int(volume_entry.get())
            self.app.seek_position = int(seek_entry.get())
            self.app.rewind_instead_prev = self.app.rewind_instead_prev_var.get()

            for key, (modifier_frame, entry, var_dict) in hotkey_entries.items():
                if key == "play/pause":
                    self.app.hotkeys[key] = update_hotkey_entry(
                        entry,
                        hotkey_vars["ctrl"][hotkey_keys.index(key)],
                        hotkey_vars["alt"][hotkey_keys.index(key)],
                        hotkey_vars["shift"][hotkey_keys.index(key)],
                        hotkey_vars["win"][hotkey_keys.index(key)],
                    )
                else:
                    self.app.hotkeys[key] = update_hotkey_entry(
                        entry,
                        hotkey_vars["ctrl"][hotkey_keys.index(key)],
                        hotkey_vars["alt"][hotkey_keys.index(key)],
                        hotkey_vars["shift"][hotkey_keys.index(key)],
                        hotkey_vars["win"][hotkey_keys.index(key)],
                    )

        def create_modifiers(frame, ctrl_var, alt_var, shift_var, win_var):
            ctrl_checkbox = ttk.Checkbutton(frame, text="Ctrl", variable=ctrl_var)
            alt_checkbox = ttk.Checkbutton(frame, text="Alt", variable=alt_var)
            shift_checkbox = ttk.Checkbutton(frame, text="Shift", variable=shift_var)
            win_checkbox = ttk.Checkbutton(frame, text="Win", variable=win_var)
            ctrl_checkbox.config(command=set_modified)
            alt_checkbox.config(command=set_modified)
            shift_checkbox.config(command=set_modified)
            win_checkbox.config(command=set_modified)
            return ctrl_checkbox, alt_checkbox, shift_checkbox, win_checkbox

        def update_devices():
            devices_data = self.app.GetDevices()
            if devices_data is None:
                return

            if "devices" in devices_data:
                devices = {
                    device["name"]: device["id"] for device in devices_data["devices"]
                }
                device_id_entry["values"] = list(devices.keys())

            def on_device_changed(event=None):
                set_modified_cred()
                device_name = device_id_entry.get()
                device_id = devices.get(device_name)
                if device_id is not None:
                    device_id_entry.set(device_id)
                    device_id_entry.selection_clear()
                    frame.focus()

            device_id_entry.bind("<<ComboboxSelected>>", on_device_changed)

        def validate_number(new_value):
            if new_value.isdigit() or new_value == "":
                return True
            return False

        def validate_volume(new_value):
            if new_value == "":
                return True
            try:
                value = int(new_value)
                if 0 <= value <= 100 and new_value == str(value):
                    return True
            except ValueError:
                return False
            return False

        def link_callback(url):
            webbrowser.open_new_tab(url)

        def center_window(window):
            window.update_idletasks()
            width = window.winfo_reqwidth()
            height = window.winfo_reqheight()
            x = (window.winfo_screenwidth() // 2) - (width // 2)
            y = (window.winfo_screenheight() // 2) - (height // 2)
            window.geometry(f"{width}x{height}+{x}+{y}")
            window.after(100, window.focus_force())

        def device_action():
            if not self.app.TokenExists():
                self.app.CreateToken()
            set_input_fields()
            update_devices()

        def save_action():
            set_input_fields()
            if self.app.SaveConfig():
                save_button.config(state=tk.DISABLED)

        def start_action():
            set_input_fields()

            if (
                client_id_entry.get() == ""
                or client_secret_entry.get() == ""
                or device_id_entry.get() == ""
            ):
                self.app.CreateToken()
                return

            if self.modified_credentials and not self.app.CreateToken():
                return
            else:
                root.destroy()
                self.app.StopHotkeyListener()
                if not self.menu.visible:
                    self.run()
                else:
                    self.app.StartHotkeyListener()

        # --------------------------------------------------------------------------------------- #
        """
        Create elements
        """
        root = ThemedTk(theme="breeze")
        root.withdraw()
        root.title("BeatBind (v1.7.0)")
        root.iconbitmap(self.icon_path)
        root.focus_force()

        # Frames
        frame = ttk.Frame(root, padding=20)
        frame.grid(column=0, row=0)
        options_frame = ttk.Frame(frame)
        source_frame = ttk.Frame(frame)
        button_frame = ttk.Frame(frame)

        # Separators
        vertical_separator = ttk.Separator(frame, orient="vertical")
        horizontal_separator = ttk.Separator(frame, orient="horizontal")

        # Labels
        client_id_label = ttk.Label(frame, text="Client ID:")
        client_secret_label = ttk.Label(frame, text="Client Secret:")
        port_label = ttk.Label(frame, text="Port:")
        device_id_label = ttk.Label(frame, text="Device ID:")

        volume_label = ttk.Label(options_frame, text="Volume (steps):")
        seek_label = ttk.Label(options_frame, text="Seek (ms):")

        play_pause_label = ttk.Label(frame, text="Play/Pause:")
        play_label = ttk.Label(frame, text="Play:")
        pause_label = ttk.Label(frame, text="Pause:")
        prev_track_label = ttk.Label(frame, text="Previous Track:")
        next_track_label = ttk.Label(frame, text="Next Track:")
        volume_up_label = ttk.Label(frame, text="Volume Up:")
        volume_down_label = ttk.Label(frame, text="Volume Down:")
        mute_label = ttk.Label(frame, text="Mute:")
        seek_forward_label = ttk.Label(frame, text="Seek Forward:")
        seek_backward_label = ttk.Label(frame, text="Seek Backward:")

        source_link = ttk.Label(
            source_frame,
            text="GitHub Source",
            font=("TkDefaultFont", 10, "underline"),
            foreground="#3DAEE9",
            cursor="hand2",
        )
        source_link.bind(
            "<Button-1>",
            lambda event: link_callback("https://github.com/justinknguyen/BeatBind"),
        )

        # Entries
        client_id_entry = ttk.Entry(frame, width=42)
        client_secret_entry = ttk.Entry(frame, width=42)
        port_entry = ttk.Spinbox(
            frame,
            from_=0,
            to=float("inf"),
            width=42,
            increment=1,
            validate="all",
            validatecommand=(root.register(validate_number), "%P"),
        )
        device_id_entry = ttk.Combobox(frame, width=40)
        volume_entry = ttk.Spinbox(
            options_frame,
            from_=0,
            to=100,
            width=5,
            increment=1,
            validate="all",
            validatecommand=(root.register(validate_volume), "%P"),
        )
        seek_entry = ttk.Spinbox(
            options_frame,
            from_=0,
            to=float("inf"),
            width=10,
            increment=1,
            validate="all",
            validatecommand=(root.register(validate_number), "%P"),
        )

        # Buttons
        devices_button = ttk.Button(frame, text="Get Devices", command=device_action)
        save_button = ttk.Button(
            button_frame, text="Save", command=save_action, state=tk.DISABLED
        )
        start_button = ttk.Button(
            button_frame, text="Start & Close", command=start_action
        )

        # Checkboxes
        self.app.startup_var = tk.BooleanVar()
        self.app.minimize_var = tk.BooleanVar()
        self.app.rewind_instead_prev_var = tk.BooleanVar()
        startup_checkbox = ttk.Checkbutton(
            frame,
            text="Start on Windows startup",
            variable=self.app.startup_var,
        )
        minimize_checkbox = ttk.Checkbutton(
            frame, text="Start minimized", variable=self.app.minimize_var
        )
        rewind_instead_prev_checkbox = ttk.Checkbutton(
            frame,
            text="Previous Track: rewind to start",
            variable=self.app.rewind_instead_prev_var,
        )

        # --------------------------------------------------------------------------------------- #

        def create_hotkey_area(
            frame,
            listen_for_key_events,
            create_modifiers,
            width=12,
            padding_x=2,
            padding_y=2,
        ):
            """
            Hotkey area
            """
            var_names = ["ctrl", "alt", "shift", "win"]
            var_dict = {name: tk.BooleanVar() for name in var_names}

            modifiers_frame = ttk.Frame(frame)
            entry = ttk.Entry(modifiers_frame, width=width, justify="center")
            entry.bind("<FocusIn>", lambda event: listen_for_key_events(entry))

            checkboxes = create_modifiers(
                modifiers_frame,
                var_dict["ctrl"],
                var_dict["alt"],
                var_dict["shift"],
                var_dict["win"],
            )

            for i, checkbox in enumerate(checkboxes):
                checkbox.grid(row=0, column=i, padx=padding_x, pady=padding_y)

            entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

            return modifiers_frame, entry, var_dict

        def initialize_entries():
            """
            Initialize entries
            """
            entries = [
                client_id_entry,
                client_secret_entry,
                port_entry,
                device_id_entry,
                volume_entry,
                seek_entry,
            ]
            keys = [
                "client_id",
                "client_secret",
                "port",
                "device_id",
                "volume",
                "seek",
                "rewind_instead_prev",
            ]
            keys_defaults = ["", "", 8888, "", 5, 5000, False]

            hotkey_entries = {}
            hotkey_vars = {"ctrl": [], "alt": [], "shift": [], "win": []}

            hotkey_keys = [
                "play/pause",
                "play",
                "pause",
                "prev_track",
                "next_track",
                "volume_up",
                "volume_down",
                "mute",
                "seek_forward",
                "seek_backward",
            ]
            hotkey_defaults = [
                "control+alt+shift+p",
                "control+alt+shift+z",
                "control+alt+shift+x",
                "control+alt+shift+left",
                "control+alt+shift+right",
                "control+alt+shift+up",
                "control+alt+shift+down",
                "control+alt+shift+space",
                "control+alt+shift+f",
                "control+alt+shift+b",
            ]

            for key in hotkey_keys:
                modifier_frame, entry, var_dict = create_hotkey_area(
                    frame, listen_for_key_events, create_modifiers
                )
                hotkey_entries[key] = (modifier_frame, entry, var_dict)
                for modifier in ["ctrl", "alt", "shift", "win"]:
                    hotkey_vars[modifier].append(var_dict[modifier])
                modifier_frame.grid(
                    row=hotkey_keys.index(key) + 9, column=1, sticky="W"
                )

            return (
                entries,
                keys,
                keys_defaults,
                hotkey_entries,
                hotkey_keys,
                hotkey_defaults,
                hotkey_vars,
            )

        (
            entries,
            keys,
            keys_defaults,
            hotkey_entries,
            hotkey_keys,
            hotkey_defaults,
            hotkey_vars,
        ) = initialize_entries()

        # --------------------------------------------------------------------------------------- #

        def load_configuration(
            config_path,
            entries,
            keys,
            keys_defaults,
            hotkey_entries,
            hotkey_keys,
            hotkey_defaults,
            hotkey_vars,
        ):
            """
            Auto-fill entries
            """
            ctrl_vars = hotkey_vars["ctrl"]
            alt_vars = hotkey_vars["alt"]
            shift_vars = hotkey_vars["shift"]
            win_vars = hotkey_vars["win"]

            if os.path.exists(config_path):
                with open(config_path, "r", encoding="utf-8") as f:
                    config = json.load(f)
                    hotkeys = config.get("hotkeys", {})

                    for entry, key, default_value in zip(entries, keys, keys_defaults):
                        autofill_entry(entry, config.get(key, default_value))

                    self.app.rewind_instead_prev_var.set(
                        config.get("rewind_instead_prev", False)
                    )

                    for (
                        (modifier_frame, entry, var_dict),
                        key,
                        default_value,
                        ctrl_var,
                        alt_var,
                        shift_var,
                        win_var,
                    ) in zip(
                        hotkey_entries.values(),
                        hotkey_keys,
                        hotkey_defaults,
                        ctrl_vars,
                        alt_vars,
                        shift_vars,
                        win_vars,
                    ):

                        modifiers = autofill_entry(
                            entry, hotkeys.get(key, default_value), hotkey=True
                        )
                        ctrl_var.set(modifiers["ctrl"])
                        alt_var.set(modifiers["alt"])
                        shift_var.set(modifiers["shift"])
                        win_var.set(modifiers["win"])

                    self.app.startup_var.set(config.get("startup", False))
                    self.app.minimize_var.set(config.get("minimize", False))
            else:
                for entry, key, default_value in zip(entries, keys, keys_defaults):
                    autofill_entry(entry, default_value)

                self.app.rewind_instead_prev_var.set(False)

                for (
                    (modifier_frame, entry, var_dict),
                    key,
                    default_value,
                    ctrl_var,
                    alt_var,
                    shift_var,
                    win_var,
                ) in zip(
                    hotkey_entries.values(),
                    hotkey_keys,
                    hotkey_defaults,
                    ctrl_vars,
                    alt_vars,
                    shift_vars,
                    win_vars,
                ):
                    modifiers = autofill_entry(entry, default_value, hotkey=True)
                    ctrl_var.set(modifiers["ctrl"])
                    alt_var.set(modifiers["alt"])
                    shift_var.set(modifiers["shift"])
                    win_var.set(modifiers["win"])

                self.app.startup_var.set(False)
                self.app.minimize_var.set(False)

        load_configuration(
            self.app.config_path,
            entries,
            keys,
            keys_defaults,
            hotkey_entries,
            hotkey_keys,
            hotkey_defaults,
            hotkey_vars,
        )

        # --------------------------------------------------------------------------------------- #
        def bind_entries_to_set_modified(
            set_modified, set_modified_cred, hotkey_entries
        ):
            """
            Bind the entries and checkboxes to enable the "Save" button when modified.
            """
            entries = [volume_entry, seek_entry]

            cred_entries = [
                client_id_entry,
                client_secret_entry,
                port_entry,
                device_id_entry,
            ]

            for entry in entries:
                entry.bind("<KeyRelease>", set_modified)

            for entry in cred_entries:
                entry.bind("<KeyRelease>", set_modified_cred)

            for modifier_frame, entry, var_dict in hotkey_entries.values():
                entry.bind("<KeyRelease>", set_modified)

            port_entry.bind("<<Increment>>", set_modified_cred)
            port_entry.bind("<<Decrement>>", set_modified_cred)
            volume_entry.bind("<<Increment>>", set_modified)
            volume_entry.bind("<<Decrement>>", set_modified)
            seek_entry.bind("<<Increment>>", set_modified)
            seek_entry.bind("<<Decrement>>", set_modified)

            rewind_instead_prev_checkbox.config(command=set_modified)
            startup_checkbox.config(command=set_modified)
            minimize_checkbox.config(command=set_modified)

        bind_entries_to_set_modified(set_modified, set_modified_cred, hotkey_entries)
        # --------------------------------------------------------------------------------------- #
        """
        Grid layout
        """
        client_id_label.grid(row=1, column=0, sticky="E")
        client_id_entry.grid(row=1, column=1, sticky="EW")
        client_secret_label.grid(row=2, column=0, sticky="E")
        client_secret_entry.grid(row=2, column=1, sticky="EW")
        port_label.grid(row=3, column=0, sticky="E")
        port_entry.grid(row=3, column=1, sticky="EW")
        device_id_label.grid(row=4, column=0, sticky="E")
        device_id_entry.grid(row=4, column=1, sticky="EW")
        devices_button.grid(row=5, column=1, sticky="EW")

        vertical_separator.grid(row=1, column=2, rowspan=5, sticky="NS")

        startup_checkbox.grid(row=1, column=3, sticky="W")
        minimize_checkbox.grid(row=2, column=3, sticky="W")
        rewind_instead_prev_checkbox.grid(row=3, column=3, sticky="W")
        options_frame.grid(row=4, column=3, columnspan=4, sticky="W")
        volume_label.grid(row=0, column=0, sticky="E")
        volume_entry.grid(row=0, column=1, sticky="W", padx=(0, 5))
        seek_label.grid(row=0, column=2, sticky="E", padx=(5, 0))
        seek_entry.grid(row=0, column=3, sticky="W")

        horizontal_separator.grid(row=6, columnspan=4, sticky="EW", pady=10)

        play_pause_label.grid(row=7, column=0, sticky="E")
        hotkey_entries["play/pause"][0].grid(row=7, column=1, sticky="W", padx=(0, 20))
        play_label.grid(row=8, column=0, sticky="E")
        hotkey_entries["play"][0].grid(row=8, column=1, sticky="W", padx=(0, 20))
        pause_label.grid(row=9, column=0, sticky="E")
        hotkey_entries["pause"][0].grid(row=9, column=1, sticky="W", padx=(0, 20))
        prev_track_label.grid(row=10, column=0, sticky="E")
        hotkey_entries["prev_track"][0].grid(row=10, column=1, sticky="W", padx=(0, 20))
        next_track_label.grid(row=11, column=0, sticky="E")
        hotkey_entries["next_track"][0].grid(row=11, column=1, sticky="W", padx=(0, 20))

        volume_up_label.grid(row=7, column=2, sticky="E")
        hotkey_entries["volume_up"][0].grid(row=7, column=3, sticky="W")
        volume_down_label.grid(row=8, column=2, sticky="E")
        hotkey_entries["volume_down"][0].grid(row=8, column=3, sticky="W")
        mute_label.grid(row=9, column=2, sticky="E")
        hotkey_entries["mute"][0].grid(row=9, column=3, sticky="W")
        seek_backward_label.grid(row=10, column=2, sticky="E")
        hotkey_entries["seek_backward"][0].grid(row=10, column=3, sticky="W")
        seek_forward_label.grid(row=11, column=2, sticky="E")
        hotkey_entries["seek_forward"][0].grid(row=11, column=3, sticky="W")

        button_frame.grid(row=12, column=0, columnspan=4, pady=10)
        save_button.pack(side="left", padx=(0, 5))
        start_button.pack(side="left", padx=(5, 0))
        source_frame.grid(row=13, column=0, columnspan=4, pady=10)
        source_link.pack(side="left")

        # Center window and focus
        center_window(root)
        root.deiconify()
        root.update()
        root.focus_force()

        if self.app.TokenExists():
            update_devices()

        # Run the GUI
        root.mainloop()

    def run(self):
        self.app.UpdateStartupRegistry()
        self.app.StartHotkeyListener()
        self.menu.run()
