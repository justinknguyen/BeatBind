import json
import json.decoder
import os
import tkinter as tk
import webbrowser
from tkinter import ttk, filedialog, font

import psutil
import pystray
import win32api
from global_hotkeys import keycodes
from PIL import Image
from ttkthemes import ThemedTk
import customtkinter as ctk
from CTkSpinbox import *


class Frontend(object):
    def __init__(self, app):
        self.app = app
        self.icon_path = app.icon_path

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
        global modifiedCredentials
        modifiedCredentials = False

        # Below method done as button parameter
        def set_modified(event=None):
            save_button = ctk.CTkButton(
                button_frame, text="Save", command=save_action, state="normal"
            )

        def set_modified_cred(event=None):
            global modifiedCredentials
            modifiedCredentials = True
            save_button.configure(state=tk.NORMAL)

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
            modifiers = {"ctrl": False, "alt": False, "shift": False}
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
            if "+" in hotkey_str:
                hotkey_str = hotkey_str.replace("+", "")
            key = hotkey_str.strip()
            return modifiers, key

        def update_hotkey_entry(entry, ctrl_var, alt_var, shift_var):
            modifiers = []
            if ctrl_var.get():
                modifiers.append("control")
            if alt_var.get():
                modifiers.append("alt")
            if shift_var.get():
                modifiers.append("shift")
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
            self.app.hotkeys["play/pause"] = update_hotkey_entry(
                play_pause_entry,
                ctrl_play_pause_var,
                alt_play_pause_var,
                shift_play_pause_var,
            )
            self.app.hotkeys["prev_track"] = update_hotkey_entry(
                prev_track_entry,
                ctrl_prev_track_var,
                alt_prev_track_var,
                shift_prev_track_var,
            )
            self.app.hotkeys["next_track"] = update_hotkey_entry(
                next_track_entry,
                ctrl_next_track_var,
                alt_next_track_var,
                shift_next_track_var,
            )
            self.app.hotkeys["volume_up"] = update_hotkey_entry(
                volume_up_entry,
                ctrl_volume_up_var,
                alt_volume_up_var,
                shift_volume_up_var,
            )
            self.app.hotkeys["volume_down"] = update_hotkey_entry(
                volume_down_entry,
                ctrl_volume_down_var,
                alt_volume_down_var,
                shift_volume_down_var,
            )
            self.app.hotkeys["mute"] = update_hotkey_entry(
                mute_entry,
                ctrl_mute_var,
                alt_mute_var,
                shift_mute_var,
            )
            self.app.hotkeys["seek_forward"] = update_hotkey_entry(
                seek_forward_entry,
                ctrl_seek_forward_var,
                alt_seek_forward_var,
                shift_seek_forward_var,
            )
            self.app.hotkeys["seek_backward"] = update_hotkey_entry(
                seek_backward_entry,
                ctrl_seek_backward_var,
                alt_seek_backward_var,
                shift_seek_backward_var,
            )
            self.app.hotkeys["shuffle"] = update_hotkey_entry(
                shuffle_entry,
                ctrl_shuffle_var,
                alt_shuffle_var,
                shift_shuffle_var,
            )

        def create_modifiers(frame, ctrl_var, alt_var, shift_var):
            ctrl_checkbox = ctk.CTkCheckBox(
                frame, text="Ctrl", variable=ctrl_var, width=20
            )
            alt_checkbox = ctk.CTkCheckBox(
                frame, text="Alt", variable=alt_var, width=20
            )
            shift_checkbox = ctk.CTkCheckBox(
                frame, text="Shift", variable=shift_var, width=20
            )
            ctrl_checkbox.configure(command=set_modified)
            alt_checkbox.configure(command=set_modified)
            shift_checkbox.configure(command=set_modified)
            return ctrl_checkbox, alt_checkbox, shift_checkbox

        def update_devices():
            devices_data = self.app.GetDevices()
            if devices_data is None:
                return

            if "devices" in devices_data:
                devices = {
                    device["name"]: device["id"] for device in devices_data["devices"]
                }
                device_id_entry["values"] = list(devices.keys())

            def on_device_changed(event):
                set_modified_cred()
                device_name = device_id_entry.get()
                device_id = devices.get(device_name)
                if device_id is not None:
                    device_id_entry.set(device_id)
                    device_id_entry.selection_clear()
                    frame.focus()

            device_id_entry.bind("<<ComboboxSelected>>", on_device_changed)

        def validate_volume(P):
            # Check if the input is empty (allowing deletion)
            if P == "":
                return True
            try:
                # Convert the string to an integer
                value = int(P)
                # Check if the value is within 0-100 and the string is a valid representation (no leading zeros)
                if 0 <= value <= 100 and P == str(value):
                    return True
            except ValueError:
                # Conversion to integer failed, meaning it's not a valid integer
                return False
            # If none of the above conditions are met, return False
            return False

        def link_callback(url):
            webbrowser.open_new_tab(url)

        def center_window(window):
            window.update_idletasks()
            width = window.winfo_reqwidth()
            width = (
                413  # window opened much larger than UI? may be issue on other machines
            )
            height = window.winfo_reqheight()
            height = 653
            x = (window.winfo_screenwidth() // 2) - (width // 2)
            y = (window.winfo_screenheight() // 2) - (height // 2)
            window.geometry(f"{width}x{height}+{x}+{y}")
            window.after(1, lambda: window.focus_force())

        def device_action():
            set_input_fields()
            if not self.app.TokenExists():
                self.app.CreateToken()
                update_devices()

        def save_action():
            set_input_fields()
            if self.app.SaveConfig():
                save_button.configure(state="disabled")

        def start_action():
            global modifiedCredentials
            set_input_fields()

            if (
                client_id_entry.get() == ""
                or client_secret_entry.get() == ""
                or device_id_entry.get() == ""
            ):
                self.app.CreateToken()
                return

            if modifiedCredentials and not self.app.CreateToken():
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
        # Dark/Light mode
        ctk.set_appearance_mode("system")
        ctk.set_default_color_theme("blue")

        root = ctk.CTk()
        # root = ThemedTk(theme="breeze")
        style = ttk.Style(root)
        # style.configure("TFrame", foreground="", background="blue")
        # style.configure("TFrame", background="red")
        root.withdraw()
        root.title("BeatBind (v1.5.3)")
        root.iconbitmap(self.icon_path)
        root.focus_force()

        # Checkboxes -- CTK
        self.app.rewind_instead_prev_var = ctk.BooleanVar()
        self.app.startup_var = ctk.BooleanVar()
        self.app.minimize_var = ctk.BooleanVar()

        # Create a frame with padding -- CTK
        frame = ctk.CTkFrame(root)
        frame.grid(column=0, row=0, padx=20)

        # Separator
        separator = ttk.Separator(frame, orient="horizontal")

        # Labels -- CTK
        client_id_label = ctk.CTkLabel(frame, text="Client ID:   ")
        client_secret_label = ctk.CTkLabel(frame, text="Client Secret:   ")
        port_label = ctk.CTkLabel(frame, text="Port:   ")
        device_id_label = ctk.CTkLabel(frame, text="Device ID:   ")

        options_frame = ctk.CTkFrame(frame)
        # Padding for custom spinbox
        volume_label = ctk.CTkLabel(options_frame, text="Volume:   ")
        seek_label = ctk.CTkLabel(options_frame, text="Seek:   ")

        play_pause_label = ctk.CTkLabel(frame, text="Play/Pause:   ")
        prev_track_label = ctk.CTkLabel(frame, text="Previous Track:   ")
        next_track_label = ctk.CTkLabel(frame, text="Next Track:   ")
        volume_up_label = ctk.CTkLabel(frame, text="Volume Up:   ")
        volume_down_label = ctk.CTkLabel(frame, text="Volume Down:   ")
        mute_label = ctk.CTkLabel(frame, text="Mute:   ")
        seek_forward_label = ctk.CTkLabel(frame, text="Seek Forward:   ")
        seek_backward_label = ctk.CTkLabel(frame, text="Seek Backward:   ")
        shuffle_label = ctk.CTkLabel(frame, text="Shuffle:")

        source_frame = ctk.CTkFrame(frame)
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

        # Entries -- CTK (Widths Change: 40 -> 245, ttk-Combo: 38->)
        client_id_entry = ctk.CTkEntry(frame, width=240)
        client_secret_entry = ctk.CTkEntry(frame, width=240)
        port_entry = ctk.CTkEntry(frame, width=240)
        device_id_entry = ttk.Combobox(frame)

        # Custom spinbox library implementation (extension of CustomTkinter)
        vcmd = (root.register(validate_volume), "%P")
        volume_entry = CTkSpinbox(
            options_frame,
            min_value=0,
            max_value=100,
            width=90,
            height=27,
            scroll_value=1,
            command=vcmd,
        )
        seek_entry = CTkSpinbox(
            options_frame,
            min_value=0,
            max_value=10000,
            width=90,
            height=27,
            scroll_value=1,
        )
        # Checkbox command addition
        rewind_instead_prev_checkbox = ctk.CTkCheckBox(
            frame,
            text="Previous Track: rewind to start",
            variable=self.app.rewind_instead_prev_var,
        )

        # Buttons -- CTK + Change state
        devices_button = ctk.CTkButton(
            frame, text="Get Devices", command=device_action, state="normal", width=240
        )
        button_frame = ctk.CTkFrame(frame)
        save_button = ctk.CTkButton(
            button_frame, text="Save", command=save_action, state="disabled"
        )
        start_button = ctk.CTkButton(
            button_frame, text="Start & Close", command=start_action, state="normal"
        )
        checkbox_frame = ctk.CTkFrame(frame)
        startup_checkbox = ctk.CTkCheckBox(
            checkbox_frame,
            text="Start on Windows startup",
            variable=self.app.startup_var,
        )
        minimize_checkbox = ctk.CTkCheckBox(
            checkbox_frame,
            text="Start minimized",
            variable=self.app.minimize_var,
        )

        # --------------------------------------------------------------------------------------- #
        """
        Hotkey area
        """
        width = 90
        padding_x = 2
        padding_y = 2

        # Control management - CTk Overhaul - Entries; width: 12->90
        ctrl_play_pause_var = ctk.BooleanVar()
        alt_play_pause_var = ctk.BooleanVar()
        shift_play_pause_var = ctk.BooleanVar()
        play_pause_modifiers = ctk.CTkFrame(frame)
        play_pause_entry = ctk.CTkEntry(
            play_pause_modifiers, width=width, justify="center"
        )
        play_pause_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(play_pause_entry)
        )
        (
            ctrl_play_pause_checkbox,
            alt_play_pause_checkbox,
            shift_play_pause_checkbox,
        ) = create_modifiers(
            play_pause_modifiers,
            ctrl_play_pause_var,
            alt_play_pause_var,
            shift_play_pause_var,
        )
        ctrl_play_pause_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_play_pause_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_play_pause_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        play_pause_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_prev_track_var = ctk.BooleanVar()
        alt_prev_track_var = ctk.BooleanVar()
        shift_prev_track_var = ctk.BooleanVar()
        prev_track_modifiers = ctk.CTkFrame(frame)
        prev_track_entry = ctk.CTkEntry(
            prev_track_modifiers, width=width, justify="center"
        )
        prev_track_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(prev_track_entry)
        )
        (
            ctrl_prev_track_checkbox,
            alt_prev_track_checkbox,
            shift_prev_track_checkbox,
        ) = create_modifiers(
            prev_track_modifiers,
            ctrl_prev_track_var,
            alt_prev_track_var,
            shift_prev_track_var,
        )
        ctrl_prev_track_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_prev_track_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_prev_track_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        prev_track_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_next_track_var = ctk.BooleanVar()
        alt_next_track_var = ctk.BooleanVar()
        shift_next_track_var = ctk.BooleanVar()
        next_track_modifiers = ctk.CTkFrame(frame)
        next_track_entry = ctk.CTkEntry(
            next_track_modifiers, width=width, justify="center"
        )
        next_track_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(next_track_entry)
        )
        (
            ctrl_next_track_checkbox,
            alt_next_track_checkbox,
            shift_next_track_checkbox,
        ) = create_modifiers(
            next_track_modifiers,
            ctrl_next_track_var,
            alt_next_track_var,
            shift_next_track_var,
        )
        ctrl_next_track_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_next_track_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_next_track_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        next_track_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_volume_up_var = ctk.BooleanVar()
        alt_volume_up_var = ctk.BooleanVar()
        shift_volume_up_var = ctk.BooleanVar()
        volume_up_modifiers = ctk.CTkFrame(frame)
        volume_up_entry = ctk.CTkEntry(
            volume_up_modifiers, width=width, justify="center"
        )
        volume_up_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(volume_up_entry)
        )
        (
            ctrl_volume_up_checkbox,
            alt_volume_up_checkbox,
            shift_volume_up_checkbox,
        ) = create_modifiers(
            volume_up_modifiers,
            ctrl_volume_up_var,
            alt_volume_up_var,
            shift_volume_up_var,
        )
        ctrl_volume_up_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_volume_up_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_volume_up_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        volume_up_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_volume_down_var = ctk.BooleanVar()
        alt_volume_down_var = ctk.BooleanVar()
        shift_volume_down_var = ctk.BooleanVar()
        volume_down_modifiers = ctk.CTkFrame(frame)
        volume_down_entry = ctk.CTkEntry(
            volume_down_modifiers, width=width, justify="center"
        )
        volume_down_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(volume_down_entry)
        )
        (
            ctrl_volume_down_checkbox,
            alt_volume_down_checkbox,
            shift_volume_down_checkbox,
        ) = create_modifiers(
            volume_down_modifiers,
            ctrl_volume_down_var,
            alt_volume_down_var,
            shift_volume_down_var,
        )
        ctrl_volume_down_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_volume_down_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_volume_down_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        volume_down_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_mute_var = ctk.BooleanVar()
        alt_mute_var = ctk.BooleanVar()
        shift_mute_var = ctk.BooleanVar()
        mute_modifiers = ctk.CTkFrame(frame)
        mute_entry = ctk.CTkEntry(mute_modifiers, width=width, justify="center")
        mute_entry.bind("<FocusIn>", lambda event: listen_for_key_events(mute_entry))
        (
            ctrl_mute_checkbox,
            alt_mute_checkbox,
            shift_mute_checkbox,
        ) = create_modifiers(
            mute_modifiers,
            ctrl_mute_var,
            alt_mute_var,
            shift_mute_var,
        )
        ctrl_mute_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_mute_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_mute_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        mute_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_seek_forward_var = ctk.BooleanVar()
        alt_seek_forward_var = ctk.BooleanVar()
        shift_seek_forward_var = ctk.BooleanVar()
        seek_forward_modifiers = ctk.CTkFrame(frame)
        seek_forward_entry = ctk.CTkEntry(
            seek_forward_modifiers, width=width, justify="center"
        )
        seek_forward_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(seek_forward_entry)
        )
        (
            ctrl_seek_forward_checkbox,
            alt_seek_forward_checkbox,
            shift_seek_forward_checkbox,
        ) = create_modifiers(
            seek_forward_modifiers,
            ctrl_seek_forward_var,
            alt_seek_forward_var,
            shift_seek_forward_var,
        )
        ctrl_seek_forward_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_seek_forward_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_seek_forward_checkbox.grid(
            row=0, column=2, padx=padding_x, pady=padding_y
        )
        seek_forward_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_seek_backward_var = ctk.BooleanVar()
        alt_seek_backward_var = ctk.BooleanVar()
        shift_seek_backward_var = ctk.BooleanVar()
        seek_backward_modifiers = ctk.CTkFrame(frame)
        seek_backward_entry = ctk.CTkEntry(
            seek_backward_modifiers, width=width, justify="center"
        )
        seek_backward_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(seek_backward_entry)
        )
        (
            ctrl_seek_backward_checkbox,
            alt_seek_backward_checkbox,
            shift_seek_backward_checkbox,
        ) = create_modifiers(
            seek_backward_modifiers,
            ctrl_seek_backward_var,
            alt_seek_backward_var,
            shift_seek_backward_var,
        )
        ctrl_seek_backward_checkbox.grid(
            row=0, column=0, padx=padding_x, pady=padding_y
        )
        alt_seek_backward_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_seek_backward_checkbox.grid(
            row=0, column=2, padx=padding_x, pady=padding_y
        )
        seek_backward_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)

        ctrl_shuffle_var = ctk.BooleanVar()
        alt_shuffle_var = ctk.BooleanVar()
        shift_shuffle_var = ctk.BooleanVar()
        shuffle_modifiers = ctk.CTkFrame(frame)
        shuffle_entry = ctk.CTkEntry(shuffle_modifiers, width=width, justify="center")
        shuffle_entry.bind(
            "<FocusIn>", lambda event: listen_for_key_events(shuffle_entry)
        )
        (
            ctrl_shuffle_checkbox,
            alt_shuffle_checkbox,
            shift_shuffle_checkbox,
        ) = create_modifiers(
            shuffle_modifiers,
            ctrl_shuffle_var,
            alt_shuffle_var,
            shift_shuffle_var,
        )
        ctrl_shuffle_checkbox.grid(row=0, column=0, padx=padding_x, pady=padding_y)
        alt_shuffle_checkbox.grid(row=0, column=1, padx=padding_x, pady=padding_y)
        shift_shuffle_checkbox.grid(row=0, column=2, padx=padding_x, pady=padding_y)
        shuffle_entry.grid(row=0, column=4, padx=(10, 0), pady=padding_y)
        # --------------------------------------------------------------------------------------- #
        """
        Auto-fill entries
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
        hotkey_entries = [
            play_pause_entry,
            prev_track_entry,
            next_track_entry,
            volume_up_entry,
            volume_down_entry,
            mute_entry,
            seek_forward_entry,
            seek_backward_entry,
            shuffle_entry,
        ]
        hotkey_keys = [
            "play/pause",
            "prev_track",
            "next_track",
            "volume_up",
            "volume_down",
            "mute",
            "seek_forward",
            "seek_backward",
            "shuffle",
        ]
        hotkey_defaults = [
            "control+alt+shift+p",
            "control+alt+shift+left",
            "control+alt+shift+right",
            "control+alt+shift+up",
            "control+alt+shift+down",
            "control+alt+shift+space",
            "control+alt+shift+f",
            "control+alt+shift+b",
            "control+alt+shift+s",
        ]

        ctrl_vars = [
            ctrl_play_pause_var,
            ctrl_prev_track_var,
            ctrl_next_track_var,
            ctrl_volume_up_var,
            ctrl_volume_down_var,
            ctrl_mute_var,
            ctrl_seek_forward_var,
            ctrl_seek_backward_var,
            ctrl_shuffle_var,
        ]
        alt_vars = [
            alt_play_pause_var,
            alt_prev_track_var,
            alt_next_track_var,
            alt_volume_up_var,
            alt_volume_down_var,
            alt_mute_var,
            alt_seek_forward_var,
            alt_seek_backward_var,
            alt_shuffle_var,
        ]
        shift_vars = [
            shift_play_pause_var,
            shift_prev_track_var,
            shift_next_track_var,
            shift_volume_up_var,
            shift_volume_down_var,
            shift_mute_var,
            shift_seek_forward_var,
            shift_seek_backward_var,
            shift_shuffle_var,
        ]

        if os.path.exists(self.app.config_path):
            with open(self.app.config_path, "r") as f:
                config = json.load(f)
                hotkeys = config["hotkeys"]

                for entry, key, default_value in zip(entries, keys, keys_defaults):
                    autofill_entry(entry, config.get(key, default_value))

                self.app.rewind_instead_prev_var.set(
                    config.get("rewind_instead_prev", False)
                )

                for entry, key, default_value, ctrl_var, alt_var, shift_var in zip(
                    hotkey_entries,
                    hotkey_keys,
                    hotkey_defaults,
                    ctrl_vars,
                    alt_vars,
                    shift_vars,
                ):
                    modifiers = autofill_entry(
                        entry, hotkeys.get(key, default_value), hotkey=True
                    )
                    ctrl_var.set(modifiers["ctrl"])
                    alt_var.set(modifiers["alt"])
                    shift_var.set(modifiers["shift"])

                self.app.startup_var.set(config.get("startup", False))
                self.app.minimize_var.set(config.get("minimize", False))
        else:
            for entry, key, default_value in zip(entries, keys, keys_defaults):
                # CTK Spinbox entry accommodation
                if (
                    entry == seek_entry
                    or entry == volume_entry
                    or entry == device_id_entry
                ):
                    continue
                autofill_entry(entry, default_value)

            self.app.rewind_instead_prev_var.set(False)

            for entry, key, default_value, ctrl_var, alt_var, shift_var in zip(
                hotkey_entries,
                hotkey_keys,
                hotkey_defaults,
                ctrl_vars,
                alt_vars,
                shift_vars,
            ):
                modifiers = autofill_entry(entry, default_value, hotkey=True)
                ctrl_var.set(modifiers["ctrl"])
                alt_var.set(modifiers["alt"])
                shift_var.set(modifiers["shift"])

            self.app.startup_var.set(False)
            self.app.minimize_var.set(False)

        # --------------------------------------------------------------------------------------- #
        """
        Enable "Save" button if any entry is modified
        """
        # Checkbox commands placed inside initialization parameters
        client_id_entry.bind("<KeyRelease>", set_modified_cred)
        client_secret_entry.bind("<KeyRelease>", set_modified_cred)
        port_entry.bind("<KeyRelease>", set_modified_cred)
        device_id_entry.bind("<KeyRelease>", set_modified_cred)
        volume_entry.bind("<KeyRelease>", set_modified)
        volume_entry.bind("<<Increment>>", set_modified)
        volume_entry.bind("<<Decrement>>", set_modified)
        seek_entry.bind("<KeyRelease>", set_modified)
        seek_entry.bind("<<Increment>>", set_modified)
        seek_entry.bind("<<Decrement>>", set_modified)
        rewind_instead_prev_checkbox.configure(command=set_modified)
        play_pause_entry.bind("<KeyRelease>", set_modified)
        prev_track_entry.bind("<KeyRelease>", set_modified)
        next_track_entry.bind("<KeyRelease>", set_modified)
        volume_up_entry.bind("<KeyRelease>", set_modified)
        volume_down_entry.bind("<KeyRelease>", set_modified)
        mute_entry.bind("<KeyRelease>", set_modified)
        seek_forward_entry.bind("<KeyRelease>", set_modified)
        seek_backward_entry.bind("<KeyRelease>", set_modified)
        shuffle_entry.bind("<KeyRelease>", set_modified())
        startup_checkbox.configure(command=set_modified)
        minimize_checkbox.configure(command=set_modified)

        # --------------------------------------------------------------------------------------- #
        """
        Grid layout
        """
        client_id_label.grid(row=1, column=0, sticky="E")
        client_id_entry.grid(row=1, column=1)
        client_secret_label.grid(row=2, column=0, sticky="E")
        client_secret_entry.grid(row=2, column=1)
        port_label.grid(row=3, column=0, sticky="E")
        port_entry.grid(row=3, column=1)
        device_id_label.grid(row=4, column=0, sticky="E")
        device_id_entry.grid(row=4, column=1)
        devices_button.grid(row=5, column=1, sticky="EW")

        options_frame.grid(row=6, column=0, columnspan=4, pady=10)
        volume_label.grid(row=0, column=0, sticky="E")
        volume_entry.grid(row=0, column=1, sticky="W", padx=(0, 5))
        seek_label.grid(row=0, column=2, sticky="E", padx=(5, 0))
        seek_entry.grid(row=0, column=3, sticky="W")
        rewind_instead_prev_checkbox.grid(row=7, column=1, sticky="W")

        separator.grid(row=8, column=0, columnspan=3, sticky="EW", pady=10)

        play_pause_label.grid(row=9, column=0, sticky="E")
        play_pause_modifiers.grid(row=9, column=1, sticky="W")
        prev_track_label.grid(row=10, column=0, sticky="E")
        prev_track_modifiers.grid(row=10, column=1, sticky="W")
        next_track_label.grid(row=11, column=0, sticky="E")
        next_track_modifiers.grid(row=11, column=1, sticky="W")
        volume_up_label.grid(row=12, column=0, sticky="E")
        volume_up_modifiers.grid(row=12, column=1, sticky="W")
        volume_down_label.grid(row=13, column=0, sticky="E")
        volume_down_modifiers.grid(row=13, column=1, sticky="W")
        mute_label.grid(row=14, column=0, sticky="E")
        mute_modifiers.grid(row=14, column=1, sticky="W")
        seek_forward_label.grid(row=15, column=0, sticky="E")
        seek_forward_modifiers.grid(row=15, column=1, sticky="W")
        seek_backward_label.grid(row=16, column=0, sticky="E")
        seek_backward_modifiers.grid(row=16, column=1, sticky="W")
        shuffle_label.grid(row=17, column=0, sticky="E")
        shuffle_modifiers.grid(row=17, column=1, sticky="W")

        button_frame.grid(row=18, column=0, columnspan=2, pady=10)
        save_button.pack(side="left", padx=(0, 5))
        start_button.pack(side="left", padx=(5, 0))

        checkbox_frame.grid(row=19, column=0, columnspan=2, pady=10)
        startup_checkbox.pack(side="left", padx=(0, 5))
        minimize_checkbox.pack(side="left", padx=(5, 0))

        source_frame.grid(row=20, column=0, columnspan=2, pady=10)
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
