# CI Setup

CI requires OneShot to be installed on the runner. This is achieved by providing an Actions secret with Steam login details:

## steamcmd

To generate the login details, steamcmd must be installed. Documentation for steamcmd can be found [here](https://developer.valvesoftware.com/wiki/SteamCMD#_Windows).

Once steamcmd is installed, log into steamcmd following the steps [here](https://partner.steamgames.com/doc/sdk/uploading#automating_steampipe) and find the `config.vdf` file.

## Actions Secrets

- `STEAM_CONFIG` should contain the entire contents of the `config.vdf` file generated above
- `STEAM_USERNAME` should contain your Steam username