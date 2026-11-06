class Scaffolder < Formula
  desc "CLI universel pour générer des projets dans tous les langages"
  homepage "https://github.com/akaletekoffilevis/Scaffolder-CLI"
  version "2.1.0"
  license "MIT"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v#{version}/scaffold-osx-arm64.tar.gz"
      sha256 "REPLACE_WITH_ACTUAL_SHA256"
    else
      url "https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v#{version}/scaffold-osx-x64.tar.gz"
      sha256 "REPLACE_WITH_ACTUAL_SHA256"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v#{version}/scaffold-linux-arm64.tar.gz"
      sha256 "REPLACE_WITH_ACTUAL_SHA256"
    else
      url "https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v#{version}/scaffold-linux-x64.tar.gz"
      sha256 "REPLACE_WITH_ACTUAL_SHA256"
    end
  end

  def install
    bin.install "scaffold"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/scaffold --version")
  end
end