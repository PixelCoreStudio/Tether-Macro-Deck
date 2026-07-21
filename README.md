# Tether
Tether is a plugin for Macro Deck its whole purpose is to trigger a HTTP request (GET or POST) directly with a press on your Macro Deck.

You can use it for many things like webhooks, smart-home control through their HTTP API or anything else that reacts on a HTTP Request.
I decided to make this plugin because I made the [Tether plugin for spicetify](https://github.com/PixelCoreStudio/Tether-Spicetify-Extension/tree/main) and I wanted to use a Macro Deck to control it.
But it can be used for any other HTTP Request.

## Features
- Custom HTTP Request action for Macro Deck
- Supports GET and POST Requests
- Runs asynchronously in the background without blocking the Macro Deck UI
- Connects With OBS
- Controls OBS

## Requirements
- Macro Deck version 2.14.1 or higher

## Usage HTTP
1. In the button add a action and select the plugin
2. Go to HTTP Request
3. Select your method GET or POST
4. Put your url you want to request
5. Save

## Usage OBS
1. Go to the plugin configuration and put in your OBS websocket data. Make sure your websocket is on.
2. Go to the OBS action you want to do.
3. Configure everything.

## Contributing
Issues and pull requests are welcome. Please briefly describe what was changed or added.

## License
This project is licensed under the [MIT License](https://github.com/PixelCoreStudio/Tether-Macro-Deck/blob/main/LICENSE)
