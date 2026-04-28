TFM      := netstandard2.1
CONFIG   := Debug
DLL      := VGStockpile.dll

BUILDDIR := VGStockpile/bin/$(CONFIG)/$(TFM)
BUILDDLL := $(BUILDDIR)/$(DLL)

GAME_DIR   := /mnt/c/Program Files (x86)/Steam/steamapps/common/Vanguard Galaxy
PLUGIN_DIR := $(GAME_DIR)/BepInEx/plugins
VGSTOCKPILE_DIR := $(PLUGIN_DIR)/VGStockpile

VGTTS_LIB := ../vanguard-galaxy-tts/VGTTS/lib

DOTNET ?= $(shell command -v dotnet 2>/dev/null || echo /tmp/dnsdk/dotnet/dotnet)

export DOTNET_ROLL_FORWARD := LatestMajor

.PHONY: all build link-asm deploy clean test

all: build

link-asm:
	@mkdir -p VGStockpile/lib
	@if [ ! -e "VGStockpile/lib/Assembly-CSharp.dll" ]; then \
		ln -sf "$(abspath $(VGTTS_LIB))/Assembly-CSharp.dll" VGStockpile/lib/Assembly-CSharp.dll ; \
		echo "Linked Assembly-CSharp.dll from $(VGTTS_LIB)" ; \
	fi

build: link-asm
	DOTNET_ROOT=$(dir $(DOTNET)) $(DOTNET) build VGStockpile/VGStockpile.csproj -c $(CONFIG)

test:
	DOTNET_ROOT=$(dir $(DOTNET)) $(DOTNET) test VGStockpile.Tests/VGStockpile.Tests.csproj -c $(CONFIG)

deploy: build
	@test -d "$(PLUGIN_DIR)" || { echo "BepInEx plugins dir not found at $(PLUGIN_DIR)" ; exit 1 ; }
	@mkdir -p "$(VGSTOCKPILE_DIR)"
	cp "$(BUILDDIR)"/*.dll "$(VGSTOCKPILE_DIR)/"
	@if [ -f "$(BUILDDIR)/VGStockpile.pdb" ]; then cp "$(BUILDDIR)/VGStockpile.pdb" "$(VGSTOCKPILE_DIR)/"; fi
	@echo "Deployed $(shell ls $(BUILDDIR)/*.dll | wc -l) DLL(s) to $(VGSTOCKPILE_DIR)"

clean:
	-$(DOTNET) clean VGStockpile/VGStockpile.csproj
	rm -rf VGStockpile/bin VGStockpile/obj VGStockpile.Tests/bin VGStockpile.Tests/obj dist/
