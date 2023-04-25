# Spotify Global Hotkeys
This background Windows application utilizes the [keyboard](https://github.com/boppreh/keyboard) module to listen for basic hotkeys, allowing users to easily control Spotify without the window focused. The app leverages the power of [Spotify's Web API](https://developer.spotify.com/documentation/web-api) through the use of [Spotipy](https://github.com/spotipy-dev/spotipy), providing seamless integration between the app and the music streaming platform.

Download the latest version from the [Releases](https://github.com/justinknguyen/Spotify-Global-Hotkeys/releases) page. <br>
If you'd like, you can build the .exe yourself with the provided build command in the .py file.

<p align="center">
<img src="image.png" width="50%" height="50%">
</p>

## Instructions
A list of default [hotkeys](#hotkeys) are provided for guidance and convenience. <br>
The app requires the user to input five fields: 
- [Spotify Username](#spotify-username)
- [Client ID](#client-id-client-secret-and-redirect-uri)
- [Client Secret](#client-id-client-secret-and-redirect-uri)
- [Redirect URI](#client-id-client-secret-and-redirect-uri)
- [Device ID](#device-id)

### Spotify Username
1. `Spotify Username` is the username of your Spotify account.

### Client ID, Client Secret, and Redirect URI
1. To obtain the `Client ID`, `Client Secret`, and `Redirect URI`, head to the following link [Spotify for Developers](https://developer.spotify.com/).
1. Sign-in and click on your profile in the top-right corner, then click on "Dashboard".
1. Click on the "Create app" button to the right.
1. Enter any "App name" you want (e.g., Spotify Global Hotkeys).
1. Enter any "App description" you want (e.g., Global hotkeys for Spotify).
1. Enter one of the following for the `Redirect URI`. You can change the port (80) if it doesn't work (e.g., 8000, 8080).
    - http://localhost:80/callback
    - http://127.0.0.1:80/callback
1. Click on the checkbox and then "Save".
1. Click on the "Settings" button to the top-right. 
1. Copy your `Client ID`, `Client Secret`, and `Redirect URI` and paste it into the app.
### Device ID
1. To obtain your `Device ID`, head to the following link [Get Available Devices](https://developer.spotify.com/documentation/web-api/reference/get-a-users-available-devices).
1. Click on the green "Try it" button to the right.
1. Under "RESPONSE SAMPLE", is a list of all of your devices used for Spotify.
1. Find the device name of your Windows PC, and copy the "id" as this is your `Device ID` (e.g., 1f0a123g9j1201nc...).
1. Paste the `Device ID` into the app.

### Hotkeys
- This program uses the [keyboard](https://github.com/boppreh/keyboard) module to listen for hotkeys.
- To help create your hotkeys, you can view the keycodes [here](https://github.com/boppreh/keyboard#keyboardkey_down).

Once you're ready, press on `Start` to save your settings and start listening for your hotkeys!

***
### NOTE: Restart the app if you change the location of the .exe file. <br>
The registry key used to start the app on Windows startup needs to be updated to the new .exe path. Restarting the app will resolve that.

### Where Is My Information Saved?
1. Press `Win+R` to bring up the "Run" menu, or type in "Run" within your Windows search bar.
1. Enter the following in the "Open" input field:
    ```
    %appdata%
    ```
1. Your information is stored locally within the `.../AppData/Roaming/.spotify_global_hotkeys` folder. It stores your configuration settings and the token information required to interact with Spotify's Web API.
