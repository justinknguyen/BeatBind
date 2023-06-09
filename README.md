# <img src="./BeatBind/icon.ico" width="4%" height="5%"> BeatBind - Spotify Global Hotkeys
![build](https://img.shields.io/badge/build-passing-brightgreen)
[![version](https://img.shields.io/badge/version-1.1.0-blue)](https://github.com/justinknguyen/BeatBind/releases/tag/v1.1.0)
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
- Spotify Premium
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
1. To obtain your `Device ID`, press the button "Get Devices" in the app once your `Client ID` and `Client Secret` are filled in.
1. Click on the drop-down arrow and select your device of choice.
    - Note: if you don't see your device listed, open the Spotify app on that device and play something, then check again.

Once you're done, click on `Save` within the app to save your settings. Click on `Start & Close` to close the window and start listening for your hotkeys!

You can open the settings again by right-clicking on the app's system tray icon.
## FAQ
- [How Do I Update The App?](#how-do-i-update-the-app)
- [How Do I Disable Certain Hotkeys?](#how-do-i-disable-certain-hotkeys)
- [Where Is My Information Saved?](#where-is-my-information-saved)
- [What Information Is Saved?](#what-information-is-saved)
- [Why Isn't The App Starting on Startup?](#why-isnt-the-app-starting-on-startup)
### How Do I Update The App?
You can just replace your existing file(s) with the updated version. The config files are still saved within the `.../AppData/Roaming/.beatbind` folder, so your settings won't be lost.

Note: If your app keeps crashing after an update, you'll have to delete the `.../AppData/Roaming/.beatbind` folder and reinput your settings. See [Where Is My Information Saved?](#where-is-my-information-saved).
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

## Donate
[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://paypal.me/OrbitUT?country.x=CA&locale.x=en_US)
