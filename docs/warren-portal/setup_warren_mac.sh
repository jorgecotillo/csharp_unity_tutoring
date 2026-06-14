#!/bin/bash
# ============================================================
#  Goblin Siege — Warren's Mac Setup Script
#  
#  What this does:
#    1. Installs Homebrew (Mac package manager) if not installed
#    2. Installs Git and GitHub CLI (gh) if not installed
#    3. Logs you into GitHub via browser (no passwords to type!)
#    4. Clones the project to your Desktop
#    5. Installs VS Code with Copilot, Live Share, and C# extensions
#    6. Opens the project in VS Code — ready to code!
#
#  How to run:
#    1. Open Terminal (Cmd+Space, type "Terminal", press Enter)
#    2. Copy-paste this whole line and press Enter:
#
#       curl -fsSL https://raw.githubusercontent.com/jorgecotillo/csharp_unity_tutoring/main/docs/warren-portal/setup_warren_mac.sh -o /tmp/setup.sh && bash /tmp/setup.sh
#
#    OR if Jorge sent you this file, run:
#       chmod +x setup_warren_mac.sh
#       ./setup_warren_mac.sh
# ============================================================

set -e

REPO_URL="https://github.com/jorgecotillo/csharp_unity_tutoring.git"
CLONE_DIR="$HOME/Desktop/csharp_unity_tutoring"

# Colors for friendly output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo ""
echo -e "${BLUE}╔══════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   ⚔️ Goblin Siege — Project Setup for Mac       ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════╝${NC}"
echo ""

# ----------------------------------------------------------
# Step 1: Check / Install Homebrew
# ----------------------------------------------------------
echo -e "${YELLOW}[Step 1/6]${NC} Checking for Homebrew..."

if command -v brew &>/dev/null; then
    echo -e "  ${GREEN}✓${NC} Homebrew is already installed."
else
    echo -e "  Installing Homebrew (you may need to enter your Mac password)..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    
    # Add Homebrew to PATH for Apple Silicon Macs
    if [[ -f "/opt/homebrew/bin/brew" ]]; then
        echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "$HOME/.zprofile"
        eval "$(/opt/homebrew/bin/brew shellenv)"
    fi
    echo -e "  ${GREEN}✓${NC} Homebrew installed!"
fi

# ----------------------------------------------------------
# Step 2: Check / Install Git
# ----------------------------------------------------------
echo -e "${YELLOW}[Step 2/6]${NC} Checking for Git..."

if command -v git &>/dev/null; then
    echo -e "  ${GREEN}✓${NC} Git is already installed."
else
    echo -e "  Installing Git..."
    brew install git
    echo -e "  ${GREEN}✓${NC} Git installed!"
fi

# ----------------------------------------------------------
# Step 3: Check / Install GitHub CLI and Login
# ----------------------------------------------------------
echo -e "${YELLOW}[Step 3/6]${NC} Checking for GitHub CLI..."

if command -v gh &>/dev/null; then
    echo -e "  ${GREEN}✓${NC} GitHub CLI is already installed."
else
    echo -e "  Installing GitHub CLI..."
    brew install gh
    echo -e "  ${GREEN}✓${NC} GitHub CLI installed!"
fi

# Check if already logged in
if gh auth status &>/dev/null; then
    LOGGED_USER=$(gh api user --jq '.login' 2>/dev/null || echo "unknown")
    echo -e "  ${GREEN}✓${NC} Already logged into GitHub as ${GREEN}${LOGGED_USER}${NC}."
    echo ""
    read -p "  Want to switch to a different account? (y/N): " SWITCH
    if [[ "$SWITCH" =~ ^[Yy]$ ]]; then
        gh auth logout --hostname github.com 2>/dev/null || true
        echo ""
        echo -e "  ${BLUE}🌐 A browser window will open — log in with your GitHub account.${NC}"
        echo ""
        gh auth login --hostname github.com --web --git-protocol https
    fi
else
    echo ""
    echo -e "  ${BLUE}🌐 A browser window will open — log in with your GitHub account.${NC}"
    echo -e "  ${BLUE}   (Just click 'Authorize' in the browser, that's it!)${NC}"
    echo ""
    gh auth login --hostname github.com --web --git-protocol https
fi

# Verify login worked
if ! gh auth status &>/dev/null; then
    echo -e "  ${RED}✗ GitHub login failed. Please try running the script again.${NC}"
    exit 1
fi

echo -e "  ${GREEN}✓${NC} GitHub login successful!"

# ----------------------------------------------------------
# Step 4: Check / Install VS Code
# ----------------------------------------------------------
echo -e "${YELLOW}[Step 4/6]${NC} Checking for VS Code..."

if command -v code &>/dev/null; then
    echo -e "  ${GREEN}✓${NC} VS Code is already installed."
else
    echo -e "  Installing VS Code..."
    brew install --cask visual-studio-code
    
    # Add 'code' command to PATH (sometimes needed on first install)
    if [[ ! -f "/usr/local/bin/code" ]] && [[ ! -f "/opt/homebrew/bin/code" ]]; then
        VSCODE_BIN="/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code"
        if [[ -f "$VSCODE_BIN" ]]; then
            sudo ln -sf "$VSCODE_BIN" /usr/local/bin/code 2>/dev/null || true
        fi
    fi
    echo -e "  ${GREEN}✓${NC} VS Code installed!"
fi

# ----------------------------------------------------------
# Step 5: Install VS Code Extensions
# ----------------------------------------------------------
echo -e "${YELLOW}[Step 5/6]${NC} Installing VS Code extensions..."

install_extension() {
    local ext_id="$1"
    local ext_name="$2"
    if code --list-extensions 2>/dev/null | grep -qi "$ext_id"; then
        echo -e "  ${GREEN}✓${NC} $ext_name already installed."
    else
        echo -e "  Installing $ext_name..."
        code --install-extension "$ext_id" --force 2>/dev/null
        echo -e "  ${GREEN}✓${NC} $ext_name installed!"
    fi
}

install_extension "GitHub.copilot"               "GitHub Copilot"
install_extension "GitHub.copilot-chat"           "GitHub Copilot Chat"
install_extension "ms-vsliveshare.vsliveshare"    "Live Share"
install_extension "ms-dotnettools.csharp"         "C# Language Support"
install_extension "ms-dotnettools.csdevkit"       "C# Dev Kit"
install_extension "visualstudiotoolsforunity.vstuc" "Unity Tools"

echo -e "  ${GREEN}✓${NC} All extensions ready!"

# ----------------------------------------------------------
# Step 6: Clone the project
# ----------------------------------------------------------
echo -e "${YELLOW}[Step 6/6]${NC} Cloning the project..."

if [[ -d "$CLONE_DIR" ]]; then
    echo -e "  ${GREEN}✓${NC} Project already exists at: $CLONE_DIR"
    echo -e "  Pulling latest changes..."
    cd "$CLONE_DIR"
    git pull
    echo -e "  ${GREEN}✓${NC} Updated to latest version!"
else
    echo -e "  Downloading project to your Desktop..."
    gh repo clone jorgecotillo/csharp_unity_tutoring "$CLONE_DIR"
    echo -e "  ${GREEN}✓${NC} Project cloned!"
fi

# ----------------------------------------------------------
# Done!
# ----------------------------------------------------------
echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   ✅ All done! Here's what to do next:          ║${NC}"
echo -e "${GREEN}╠══════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║                                                  ║${NC}"
echo -e "${GREEN}║   1. VS Code is opening your project now...      ║${NC}"
echo -e "${GREEN}║   2. Sign into Copilot (click the Copilot        ║${NC}"
echo -e "${GREEN}║      icon in the bottom-right of VS Code)        ║${NC}"
echo -e "${GREEN}║   3. For Unity: Open Unity Hub → Add project     ║${NC}"
echo -e "${GREEN}║      from disk → open the Goblin Siege Unity     ║${NC}"
echo -e "${GREEN}║      project (we create it in Session 1!)        ║${NC}"
echo -e "${GREEN}║   4. Have fun coding! 🎮                          ║${NC}"
echo -e "${GREEN}║                                                  ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "Project location: ${BLUE}$CLONE_DIR${NC}"
echo -e "Your dev hub:     ${BLUE}https://jorgecotillo.github.io/csharp_unity_tutoring/warren-portal/${NC}"
echo ""

# Open VS Code in the project folder
echo -e "${BLUE}Opening VS Code...${NC}"
code "$CLONE_DIR" 2>/dev/null &
