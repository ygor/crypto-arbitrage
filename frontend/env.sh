#!/bin/bash

# Recreate env-config.js file
ENV_CONFIG_FILE=/usr/share/nginx/html/env-config.js
rm -f $ENV_CONFIG_FILE
touch $ENV_CONFIG_FILE

# Add assignment 
echo "window.ENV = {" >> $ENV_CONFIG_FILE

# Read each env var that starts with REACT_APP_
for envvar in $(env | grep -o "^REACT_APP_[^=]*"); do
  # Get the value of this env var
  value=$(printenv "$envvar")
  
  # Remove REACT_APP_ prefix from the name
  name=${envvar#REACT_APP_}
  
  # Convert to camelCase (if needed)
  name="$(echo $name | sed -r 's/(_)([a-z])/\U\2/g' | sed -r 's/(.)/\L\1/')"
  
  # Add it to env-config.js
  echo "  $name: \"$value\"," >> $ENV_CONFIG_FILE
done

# Close the object
echo "};" >> $ENV_CONFIG_FILE

echo "Generated env-config.js with environment variables"
cat $ENV_CONFIG_FILE 