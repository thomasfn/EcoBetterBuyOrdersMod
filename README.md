# Eco Better Buy Orders
A server mod for Eco 9.5 that changes buy orders in stores to consider inventory and financial limits.

## Installation
1. Download `EcoBetterBuyOrdersMod.dll` from the [latest release](https://github.com/thomasfn/EcoBetterBuyOrdersMod/releases).
2. Copy the `EcoBetterBuyOrdersMod.dll` file to `Mods` folder of the dedicated server.
3. Restart the server.

## Usage

The mod will begin working automatically when installed. It can safely be added to existing saves or removed at any time. Any buy orders on stores will be automatically limited in the following situations:
- If the linked output inventories do not have enough space to store the requested quantity of the order
- If the linked bank account does not have enough currency to purchase the requested quantity of the order

Manual limits set on buy orders are still respected as before. Sell order behaviour is not changed.

## Config

There are no config properties at present for this mod.

## Building Mod from Source

### Windows

1. Login to the [Eco Website](https://play.eco/) and download the latest modkit
2. Extract the modkit and copy the dlls from `ReferenceAssemblies` to `eco-dlls` in the root directory (create the folder if it doesn't exist)
3. Open `EcoBetterBuyOrdersMod.sln` in Visual Studio 2019
4. Build the `EcoBetterBuyOrdersMod` project in Visual Studio
5. Find the artifact in `EcoBetterBuyOrdersMod\bin\{Debug|Release}\net5.0`

### Linux

1. Run `ECO_BRANCH="release" MODKIT_VERSION="0.9.5.2-beta" fetch-eco-reference-assemblies.sh` (change the modkit branch and version as needed)
2. Enter the `EcoBetterBuyOrdersMod` directory and run:
`dotnet restore`
`dotnet build`
3. Find the artifact in `EcoBetterBuyOrdersMod/bin/{Debug|Release}/net5.0`

## License
[MIT](https://choosealicense.com/licenses/mit/)