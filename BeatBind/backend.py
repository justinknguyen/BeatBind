import json
import json.decoder
import os
import sys
import threading
import time
import tkinter.messagebox as messagebox
import winreg as reg
from datetime import datetime

import requests
import win32api
import win32con
import win32gui
from global_hotkeys import *
from spotipy.oauth2 import SpotifyOAuth, SpotifyOauthError


class Backend(object):
    def __init__(self):
        def resource_path(relative_path):
            if getattr(sys, "frozen", False):
                # running as an application
                base_path = sys._MEIPASS
            else:
                # running as a standalone script
                base_path = os.path.dirname(os.path.abspath(__file__))
            return os.path.join(base_path, relative_path)

        # Create paths
        self.app_folder = os.path.dirname(os.path.abspath(__file__))
        os.makedirs(self.app_folder, exist_ok=True)
        self.config_path = os.path.join(self.app_folder, "beatbind-config.json")
        self.icon_path = resource_path("icon.ico")

        # Spotify credentials
        self.client_id = None
        self.client_secret = None
        self.port = 8888
        self.device_id = None

        # Global hotkeys
        self.hotkeys = {
            "play/pause": None,
            "prev_track": None,
            "next_track": None,
            "volume_up": None,
            "volume_down": None,
            "mute": None,
            "seek_forward": None,
            "seek_backward": None,
            "shuffle": None,
        }

        # Rewind instead of going to previous song settings
        self.rewind_instead_prev_var = None
        self.rewind_instead_prev = False
        self.rewind_threshold = 3000

        # Volume adjustment
        self.volume = 5
        self.last_volume = None
        self.muted_volume = None

        # Seek position
        self.seek_position = 5000

        # Startup and minimize
        self.startup_var = None
        self.minimize_var = None

        # Spotify access token
        self.auth_manager = None
        self.token_data = None
        self.token = None
        self.expires_in = None

        # Thread flags
        self.refresh_thread_running = False

        # Set up wake-up event listener
        app_class = win32gui.WNDCLASS()
        app_class.lpfnWndProc = self.WndProc
        app_class.lpszClassName = "BeatBind"
        app_class.hInstance = win32api.GetModuleHandle()
        class_atom = win32gui.RegisterClass(app_class)
        self.hwnd = win32gui.CreateWindowEx(
            0, class_atom, None, 0, 0, 0, 0, 0, None, None, app_class.hInstance, None
        )

    # --------------------------------------------------------------------------------------- #
    """
    API Calls
    """

    def PlayPause(self):
        if self.token:
            self.CheckTokenExpiry()

            is_playing = self.GetPlaybackState()
            if is_playing is None:
                return

            headers = {"Authorization": "Bearer " + self.token}
            if is_playing:
                # Pause the music
                url = f"https://api.spotify.com/v1/me/player/pause?device_id={self.device_id}"
            else:
                # Play the music
                url = f"https://api.spotify.com/v1/me/player/play?device_id={self.device_id}"
            try:
                response = requests.put(url, headers=headers, timeout=5)
                response.raise_for_status()
                if is_playing:
                    print("Paused music")
                else:
                    print("Playing music")
            except Exception as e:
                print(f"Error: {e}")

    def PrevNext(self, command):
        if self.token:
            self.CheckTokenExpiry()

            rewind = (
                self.rewind_instead_prev
                and command == "previous"
                and self.GetCurrentPlaybackPosition() > self.rewind_threshold
            )

            headers = {"Authorization": "Bearer " + self.token}
            url = f"https://api.spotify.com/v1/me/player/{command}?device_id={self.device_id}"

            # if more than self.rewind_threshold ms have passed since the beginning of the song,
            # rewind to the beginning instead of changing to previous song
            if rewind:
                url = "https://api.spotify.com/v1/me/player/seek?position_ms=0"

            try:
                if rewind:
                    response = requests.put(url, headers=headers, timeout=5)
                else:
                    response = requests.post(url, headers=headers, timeout=5)
                response.raise_for_status()
                if command == "previous":
                    print("Previous track")
                else:
                    print("Next track")
            except Exception as e:
                print(f"Error: {e}")

    def AdjustVolume(self, amount):
        if self.token:
            self.CheckTokenExpiry()

            headers = {"Authorization": "Bearer " + self.token}
            self.last_volume = self.GetCurrentVolume()
            if self.last_volume is None:
                self.last_volume = 50  # assume 50%

            if (self.last_volume - self.volume) < 0:
                self.last_volume = self.volume
            elif (self.last_volume + self.volume) > 100:
                self.last_volume = 100 - self.volume
            url = f"https://api.spotify.com/v1/me/player/volume?volume_percent={self.last_volume + amount}&device_id={self.device_id}"
            try:
                response = requests.put(url, headers=headers, timeout=5)
                response.raise_for_status()
                if amount > 0:
                    print("Volume up")
                else:
                    print("Volume down")
            except Exception as e:
                print(f"Error: {e}")

    def Mute(self):
        if self.token:
            self.CheckTokenExpiry()

            headers = {"Authorization": "Bearer " + self.token}
            if self.GetCurrentVolume() != 0:
                self.muted_volume = self.GetCurrentVolume()
                url = f"https://api.spotify.com/v1/me/player/volume?volume_percent=0&device_id={self.device_id}"
                print("Muting")
            else:
                url = f"https://api.spotify.com/v1/me/player/volume?volume_percent={self.muted_volume}&device_id={self.device_id}"
                print("Unmuting")
            try:
                response = requests.put(url, headers=headers, timeout=5)
                response.raise_for_status()
            except Exception as e:
                print(f"Error: {e}")

    def SeekForward(self):
        if self.token:
            self.CheckTokenExpiry()

            headers = {"Authorization": "Bearer " + self.token}
            position = self.GetCurrentPlaybackPosition() + self.seek_position
            url = f"https://api.spotify.com/v1/me/player/seek?position_ms={position}"
            try:
                response = requests.put(url, headers=headers, timeout=5)
                response.raise_for_status()
                print("Seeking forward")
            except Exception as e:
                print(f"Error: {e}")

    def SeekBackward(self):
        if self.token:
            self.CheckTokenExpiry()

            headers = {"Authorization": "Bearer " + self.token}
            position = self.GetCurrentPlaybackPosition() - self.seek_position
            if position < 0:
                position = 0
            url = f"https://api.spotify.com/v1/me/player/seek?position_ms={position}"
            try:
                response = requests.put(url, headers=headers, timeout=5)
                response.raise_for_status()
                print("Seeking backward")
            except Exception as e:
                print(f"Error: {e}")

    def Shuffle(self):
        if self.token:
            self.CheckTokenExpiry()

            is_shuffled = self.GetShuffleState()
            if is_shuffled is None:
                return

            headers = {"Authorization": "Bearer " + self.token}
            if is_shuffled:
                # Unshuffle the music
                url = f"https://api.spotify.com/v1/me/player/shuffle?state=false"
            else:
                # Shuffle the music
                url = f"https://api.spotify.com/v1/me/player/shuffle?state=true"
            try:
                response = requests.put(url, headers=headers, timeout=5)
                response.raise_for_status()
                if is_shuffled:
                    print("Unshuffling music")
                else:
                    print("Shuffling music")
            except Exception as e:
                print(f"Error: {e}")

    def GetCurrentPlaybackPosition(self):
        if self.token:
            self.CheckTokenExpiry()

            headers = {"Authorization": "Bearer " + self.token}
            url = "https://api.spotify.com/v1/me/player"
            try:
                response = requests.get(url, headers=headers, timeout=5)
                response.raise_for_status()
                data = response.json()
                if data:
                    return data["progress_ms"]
                else:
                    print("Unable to obtain playback information")
                    return 0
            except Exception as e:
                print(f"Error fetching current playback position: {e}")
                return 0

    def GetCurrentVolume(self):
        if self.token:
            headers = {"Authorization": "Bearer " + self.token}
            url = "https://api.spotify.com/v1/me/player"
            try:
                response = requests.get(url, headers=headers, timeout=5)
                response.raise_for_status()
                data = response.json()
                volume = data["device"]["volume_percent"]
                return volume
            except Exception as e:
                print(f"Error: {e}")
                return self.last_volume

    def GetPlaybackState(self):
        if self.token:
            headers = {"Authorization": "Bearer " + self.token}
            url = "https://api.spotify.com/v1/me/player"
            try:
                response = requests.get(url, headers=headers, timeout=5)
                response.raise_for_status()
                playback_data = response.json()
                return playback_data["is_playing"]
            except Exception as e:
                print(f"Error fetching playback state: {e}")
                self.HandleConnectionError()
                return None

    def GetShuffleState(self):
        if self.token:
            headers = {"Authorization": "Bearer " + self.token}
            url = "https://api.spotify.com/v1/me/player"
            try:
                response = requests.get(url, headers=headers, timeout=5)
                response.raise_for_status()
                shuffle_data = response.json()
                return shuffle_data["shuffle_state"]
            except Exception as e:
                print(f"Error fetching shuffle state: {e}")
                self.HandleConnectionError()
                return None

    def GetDevices(self):
        if self.token:
            headers = {"Authorization": "Bearer " + self.token}
            url = "https://api.spotify.com/v1/me/player/devices"
            try:
                response = requests.get(url, headers=headers, timeout=5)
                response.raise_for_status()
                return response.json()
            except Exception as e:
                print(f"Error fetching devices: {e}")

    def HandleConnectionError(self):
        # Connection error occurs when device is no longer active, so play music on specific device id
        headers = {"Authorization": "Bearer " + self.token}
        url = f"https://api.spotify.com/v1/me/player/play?device_id={self.device_id}"
        try:
            response = requests.put(url, headers=headers, timeout=5)
            if response.status_code == 403:  # The music is already playing
                # pause music on specific device id
                url = f"https://api.spotify.com/v1/me/player/pause?device_id={self.device_id}"
                response = requests.put(url, headers=headers, timeout=5)
                response.raise_for_status()
                print("Paused music")
                return
            response.raise_for_status()
            print("Playing music")
        except Exception as e:
            print(f"Error: {e}")

    # --------------------------------------------------------------------------------------- #
    """
    Configurations
    """

    def ErrorMessage(self, message):
        messagebox.showerror("Error", message)

    def WndProc(self, hWnd, message, wParam, lParam):
        if message == win32con.WM_POWERBROADCAST:
            if (
                wParam == win32con.PBT_APMRESUMEAUTOMATIC
            ):  # System is waking up from sleep
                print("System woke up from sleep")
                self.RefreshToken()
        return win32gui.DefWindowProc(hWnd, message, wParam, lParam)

    def UpdateStartupRegistry(self):
        key_path = r"Software\Microsoft\Windows\CurrentVersion\Run"
        app_name = "BeatBind"
        exe_path = os.path.realpath(sys.argv[0])
        key = reg.OpenKey(reg.HKEY_CURRENT_USER, key_path, 0, reg.KEY_ALL_ACCESS)

        try:
            registry_value, _ = reg.QueryValueEx(key, app_name)
            if registry_value != exe_path:
                reg.SetValueEx(key, app_name, 0, reg.REG_SZ, exe_path)
        except FileNotFoundError:
            print("Could not find the startup registry key")

        reg.CloseKey(key)

    def SetStartup(self, enabled):
        key = reg.HKEY_CURRENT_USER
        sub_key = r"Software\Microsoft\Windows\CurrentVersion\Run"
        app_name = "BeatBind"

        with reg.OpenKey(key, sub_key, 0, reg.KEY_ALL_ACCESS) as reg_key:
            if enabled:
                exe_path = os.path.realpath(sys.argv[0])
                reg.SetValueEx(reg_key, app_name, 0, reg.REG_SZ, exe_path)
            else:
                try:
                    reg.DeleteValue(reg_key, app_name)
                except FileNotFoundError:
                    pass

    def StopHotkeyListener(self):
        stop_checking_hotkeys()
        clear_hotkeys()

    def StartHotkeyListener(self):
        print("Listening to hotkeys...")

        # Our keybinding event handlers.
        def play_pause():
            self.PlayPause()

        def previous_track():
            self.PrevNext("previous")

        def next_track():
            self.PrevNext("next")

        def volume_up():
            self.AdjustVolume(int(self.volume))

        def volume_down():
            self.AdjustVolume(-int(self.volume))

        def mute():
            self.Mute()

        def seek_forward():
            self.SeekForward()

        def seek_backward():
            self.SeekBackward()

        def shuffle():
            self.Shuffle()

        # Create the bindings list, removing any empty hotkeys
        bindings = []
        for hotkey_name, hotkey_func in [
            ("play/pause", play_pause),
            ("prev_track", previous_track),
            ("next_track", next_track),
            ("volume_up", volume_up),
            ("volume_down", volume_down),
            ("mute", mute),
            ("seek_forward", seek_forward),
            ("seek_backward", seek_backward),
            ("shuffle", shuffle),
        ]:
            hotkey = self.hotkeys[hotkey_name].split("+")
            if all(hotkey):
                if hotkey_name == "volume_up" or hotkey_name == "volume_down":
                    bindings.append([hotkey, hotkey_func, None])
                else:
                    bindings.append([hotkey, None, hotkey_func])

        # Register all of our keybindings
        register_hotkeys(bindings)

        # Finally, start listening for keypresses
        start_checking_hotkeys()

    def SaveConfig(self):
        print("Saving config")
        # Add the hotkeys to the config dictionary
        config = {
            "startup": self.startup_var.get(),
            "minimize": self.minimize_var.get(),
            "client_id": self.client_id,
            "client_secret": self.client_secret,
            "port": self.port,
            "device_id": self.device_id,
            "volume": self.volume,
            "seek": self.seek_position,
            "rewind_instead_prev": self.rewind_instead_prev,
            "hotkeys": self.hotkeys,
        }
        try:
            # Save the config to the file with indentation for readability
            with open(self.config_path, "w") as f:
                json.dump(config, f, indent=4)
            print("Config saved")
            return True
        except IOError as e:
            print(f"Error saving config: {e}")
        except Exception as e:
            print(f"Unexpected error while saving config: {e}")
        return False

    # --------------------------------------------------------------------------------------- #
    """
    Spotify Token Management
    """

    def TokenExists(self):
        cache_file = os.path.join(self.app_folder, ".cache")
        return os.path.exists(cache_file)

    def StartupTokenRefresh(self):
        cache_file = os.path.join(self.app_folder, ".cache")
        if os.path.exists(self.config_path):
            with open(self.config_path, "r") as f:
                config = json.load(f)
                try:
                    self.auth_manager = SpotifyOAuth(
                        scope="user-modify-playback-state,user-read-playback-state",
                        client_id=config.get("client_id", ""),
                        client_secret=config.get("client_secret", ""),
                        redirect_uri=f"http://localhost:{self.port}/callback",
                        cache_path=cache_file,
                    )
                except:
                    print("Invalid config.")
                    return

            self.RefreshToken()
            # Start the loop to refresh the token before it expires, if not already running
            if not self.refresh_thread_running:
                print("Created refresh thread")
                refresh_thread = threading.Thread(target=self.RefreshTokenThread)
                refresh_thread.daemon = True
                refresh_thread.start()
                self.refresh_thread_running = True
        else:
            print("Could not find config file. Creating token...")
            self.CreateToken()

    def CheckTokenExpiry(self):
        cache_file = os.path.join(self.app_folder, ".cache")
        if os.path.exists(cache_file):
            with open(cache_file, "r") as f:
                cache_data = json.load(f)
            expires_at = cache_data["expires_at"]
            if datetime.now().timestamp() >= expires_at:
                print("Cached token has expired")
                with open(self.config_path, "r") as f:
                    config = json.load(f)
                    self.auth_manager = SpotifyOAuth(
                        scope="user-modify-playback-state,user-read-playback-state",
                        client_id=config.get("client_id", ""),
                        client_secret=config.get("client_secret", ""),
                        redirect_uri=f"http://localhost:{self.port}/callback",
                        cache_path=cache_file,
                    )
                self.RefreshToken()
        else:
            print("Could not find .cache file. Creating token...")
            self.CreateToken()

    def RefreshToken(self):
        print("Refreshing token")
        self.token_data = self.auth_manager.refresh_access_token(
            self.auth_manager.get_cached_token()["refresh_token"]
        )
        self.token = self.token_data["access_token"]
        self.expires_in = self.token_data["expires_in"]

    def RefreshTokenThread(self):
        while True:
            time.sleep(
                self.expires_in - 60
            )  # Sleep until the token needs to be refreshed
            self.RefreshToken()

    def CreateToken(self):
        print("Creating token")

        # Check if Client ID or Secret are correct
        try:
            response = requests.post(
                "https://accounts.spotify.com/api/token",
                data={"grant_type": "client_credentials"},
                auth=(self.client_id, self.client_secret),
                timeout=5,
            )
            if response.status_code == 200:
                pass
            else:
                self.ErrorMessage(response.content)
                return False
        except requests.exceptions.RequestException as e:
            self.ErrorMessage(e)
            return False

        cache_file = os.path.join(self.app_folder, ".cache")
        # Delete cache file if it exists
        if os.path.exists(cache_file):
            os.remove(cache_file)
        try:
            self.auth_manager = SpotifyOAuth(
                scope="user-modify-playback-state,user-read-playback-state",
                client_id=self.client_id,
                client_secret=self.client_secret,
                redirect_uri=f"http://localhost:{self.port}/callback",
                cache_path=cache_file,
            )
        except SpotifyOauthError as e:
            self.ErrorMessage(e)
            return False

        try:
            self.token_data = self.auth_manager.get_access_token()
        except SpotifyOauthError as e:
            self.ErrorMessage(e)
            return False

        self.token = self.token_data["access_token"]
        self.expires_in = self.token_data["expires_in"]

        # Start the loop to refresh the token before it expires, if not already running
        if not self.refresh_thread_running:
            print("Created refresh thread")
            refresh_thread = threading.Thread(target=self.RefreshTokenThread)
            refresh_thread.daemon = True
            refresh_thread.start()
            self.refresh_thread_running = True

        return True
