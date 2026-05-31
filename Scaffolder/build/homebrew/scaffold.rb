cask "scaffold" do
  version "2.0.0"

  on_macos do
    on_intel do
      url "https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v#{version}/scaffold-osx-x64.tar.gz"
      sha256 "LIVE_CHECKSUM"
    end
    on_arm do
      url "https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v#{version}/scaffold-osx-arm64.tar.gz"
      sha256 "LIVE_CHECKSUM"
    end
  end

  on_linux do
    on_intel do
      url "https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v#{version}/scaffold-linux-x64.tar.gz"
      sha256 "LIVE_CHECKSUM"
    end
  end

  name "scaffold"
  desc "CLI universel pour generer des projets dans tous les langages"
  homepage "https://github.com/akaletekoffilevis/Scaffolder-CLI"

  binary "scaffold"
end
