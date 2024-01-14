aiur() { arg="$( cut -d ' ' -f 2- <<< "$@" )" && curl -sL https://gitlab.aiursoft.cn/aiursoft/aiurscript/-/raw/master/$1.sh | sudo bash -s $arg; }

app_name="moongladepure"
repo_path="https://gitlab.aiursoft.cn/aiursoft/moongladepure"
proj_path="src/Moonglade.Web/MoongladePure.Web.csproj"

get_dll_name()
{
    filename=$(basename -- "$proj_path")
    project_name="${filename/.csproj/}"
    dll_name="$project_name.dll"
    echo $dll_name
}

install()
{
    port=$1
    if [ -z "$port" ]; then
        port=$(aiur network/get_port)
    fi
    echo "Installing $app_name... to port $port"

    # Install prerequisites    
    aiur install/dotnet
    aiur install/node

    # Clone the repo
    aiur git/clone_to $repo_path /tmp/repo

    # Install node modules
    wwwrootPath=$(dirname "/tmp/repo/$proj_path")/wwwroot
    if [ -d "$wwwrootPath" ]; then
        echo "Found wwwroot folder $wwwrootPath, will install node modules."
        npm install --prefix "$wwwrootPath" -force
    fi

    # Publish the app
    aiur dotnet/publish "/tmp/repo/$proj_path" "/opt/apps/$app_name"

    # Install fonts    
    sudo mkdir /usr/share/fonts
    sudo cp /tmp/repo/assets/OpenSans-Regular.ttf /usr/share/fonts/OpenSans-Regular.ttf
    sudo chown www-data:www-data /usr/share/fonts/OpenSans-Regular.ttf

    # Register the service
    dll_name=$(get_dll_name)
    aiur services/register_aspnet_service $app_name $port "/opt/apps/$app_name" $dll_name

    # Clean up
    echo "Install $app_name finished! Please open http://$(hostname):$port to try!"
    rm /tmp/repo -rf
}

# This will install this app under /opt/apps and register a new service with systemd.
# Example: install 5000
install "$@"