# <img src="./BeatBind/icon.ico" width="4%" height="5%"> BeatBind - Spotify Global Hotkeys
![build](https://img.shields.io/badge/build-passing-brightgreen)
[![version](https://img.shields.io/badge/version-0.6.2-blue)](https://github.com/justinknguyen/BeatBind/releases/tag/v0.6.2)
![python](https://img.shields.io/badge/python-3.10.9-yellow)
[![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/justinknguyen/BeatBind/issues)


Are you tired of constantly switching back and forth between your game and Spotify just to adjust the volume? With this app, you can finally control Spotify and adjust it's volume separately, all without the hassle of alt+tabbing. Say goodbye to interruptions and hello to effortless audio management!

This background Python Windows application utilizes the [global_hotkeys](https://github.com/btsdev/global_hotkeys) module to listen for basic hotkeys, allowing users to easily control Spotify without the window focused. The app leverages the power of [Spotify's Web API](https://developer.spotify.com/documentation/web-api) through the use of [Spotipy](https://github.com/spotipy-dev/spotipy), providing seamless integration between the app and the music streaming platform.

<p align="center">
<img src="./images/view.png" width="40%" height="40%">
</p>

## Download
Download the latest version from the [Releases](https://github.com/justinknguyen/Spotify-Global-Hotkeys/releases) page.

You can build the `.exe` yourself with the provided build command in the `build.py` file.
## Requirements
- Windows 10/11
- Spotify on your device of choice
## Instructions
The app requires the user to input three fields: 
- [Client ID](#client-id-and-client-secret)
- [Client Secret](#client-id-and-client-secret)
- [Device ID](#device-id)
### Client ID and Client Secret
1. To obtain the `Client ID` and `Client Secret`, head to the following link [Spotify for Developers](https://developer.spotify.com/).
1. Sign-in and click on your profile in the top-right corner, then click on "Dashboard".
1. Click on the "Create app" button to the right.
1. Enter any "App name" and "App description" you want. Then enter the following for the "Redirect URI":
    ```
    http://localhost:8888/callback
    ```
1. Click on the checkbox and then "Save".
    <p align="center">
    <img src="./images/create-app.png" width="100%" height="100%">
    </p>
1. Click on the "Settings" button to the top-right.
1. Copy your `Client ID` and `Client Secret` (press "View client secret") and paste it into the app.
    <p align="center">
    <img src="./images/id-and-secret.png" width="100%" height="100%">
    </p>
### Device ID
1. To obtain your `Device ID`, head to the following link [Get Available Devices](https://developer.spotify.com/documentation/web-api/reference/get-a-users-available-devices).
1. Click on the green "Try it" button to the right.
1. Choose any device you want to control under "RESPONSE SAMPLE", and copy the "id" which is the `Device ID`.
    - Note: if you don't see any devices listed, just play music on one of your devices and try again. 
1. Paste the `Device ID` into the app.
    <p align="center">
    <img src="./images/devices.png" width="100%" height="100%">
    </p>

Once you're done, click on `Save` within the app to save your settings. Click on `Start & Close` to close the window and start listening for your hotkeys!

You can open the settings again by right-clicking on the app's system tray icon.
## FAQ
### How Do I Disable Certain Hotkeys?
1. Uncheck all of the `Modifiers` checkboxes.
2. In the `Key` field, press "Backspace" or "Delete" on your keyboard to clear the field.
    <p>
    <img src="./images/unbind.png" width="70%" height="70%">
    </p>
### Where Is My Information Saved?
1. Press `Win+R` to bring up the "Run" menu, or type in "Run" within your Windows search bar.
1. Enter the following in the "Open" input field:
    ```
    %appdata%
    ```
1. Your information is stored locally within the `.../AppData/Roaming/.beatbind` folder. It stores your configuration settings and the token information required to interact with Spotify's Web API.
### What Information Is Saved?
There are two files stored within the `.../AppData/Roaming/.beatbind` folder:
- `config.json`, which contains your Client ID, Secret, Device ID, and your hotkey combinations.
- `.cache`, which contains your token information to communicate with the Spotify app.
### Why Isn't The App Starting on Startup?
You likely changed the location of the app file. The registry key used to start the app on Windows startup needs to be updated to the new `.exe` path. Starting the app again will update the path in the registry key and should resolve the issue.
