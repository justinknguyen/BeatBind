import json
import json.decoder
import os
import sys
import threading

import psutil
import win32gui
from backend import *
from frontend import *


def main():
    # If the user trys to open another instance, do not open
    count = 0
    for process in psutil.process_iter(["name"]):
        if process.info["name"] == "BeatBind.exe":
            count += 1
    if count > 1:
        sys.exit()

    try:
        backend = Backend()
        frontend = Frontend(backend)

        # Check if there is a config file
        if os.path.exists(backend.config_path):
            with open(backend.config_path, "r", encoding="utf-8") as f:
                config = json.load(f)
                hotkeys = config["hotkeys"]

                # Load the config settings into the backend
                for key, default_value in [
                    ("client_id", ""),
                    ("client_secret", ""),
                    ("port", 8888),
                    ("device_id", ""),
                    ("volume", 5),
                    ("seek", 5000),
                    ("rewind_instead_prev", False),
                ]:
                    setattr(backend, key, config.get(key, default_value))

                for key, default_value in [
                    ("play/pause", "control+alt+shift+p"),
                    ("play", "control+alt+shift+z"),
                    ("pause", "control+alt+shift+x"),
                    ("prev_track", "control+alt+shift+left"),
                    ("next_track", "control+alt+shift+right"),
                    ("volume_up", "control+alt+shift+up"),
                    ("volume_down", "control+alt+shift+down"),
                    ("mute", "control+alt+shift+space"),
                    ("seek_forward", "control+alt+shift+f"),
                    ("seek_backward", "control+alt+shift+b"),
                ]:
                    backend.hotkeys[key] = hotkeys.get(key, default_value)

            # Refresh token after successfully loading config
            backend.StartupTokenRefresh()

            # Check minimize flag and start frontend accordingly
            if config.get("minimize", False) and not frontend.menu.visible:
                frontend.run()
            else:
                frontend.SettingsWindow()
        else:
            # If config file does not exist, open the settings window
            logging.warning("Config file not found, opening Settings window")
            frontend.SettingsWindow()

        # Start message pump to process Windows messages
        def start_message_pump():
            win32gui.PumpMessages()

        message_pump_thread = threading.Thread(target=start_message_pump)
        message_pump_thread.daemon = True
        message_pump_thread.start()
        logging.info("Beatbind initialized")
    except json.JSONDecodeError as e:
        logging.error(f"Error decoding JSON in config file: {e}")
        backend.ErrorMessage(f"Error decoding JSON in config file: {e}")
        logging.info("Exiting app")
        sys.exit()
    except Exception as e:
        logging.info(f"Error initializing app: {e}")
        backend.ErrorMessage(e)
        logging.info("Exiting app")
        sys.exit()


if __name__ == "__main__":
    main()
