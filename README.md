### Thermometer

Basic CLI Utility to manage CPU frequency that works across multiple different linux distrobutions.

### Install

For this installation we will use the AUR package manager "yay".

`yay -S thermometer`

OR

You can compile from source

```$ git clone https://github.com/watchmypizza/thermometer.git"

$ cd thermometer

$ sudo pacman -S dotnet-sdk dotnet-runtime

$ dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true
```

### Roadmap

- [x] Write max cpu frequency
- [x] Write min cpu frequency
- [ ] More features to come such as GUI

### Disclaimer

The project maintainer(s) cannot be held liable or responsible for damages caused by using this tool.