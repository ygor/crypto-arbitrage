#!/bin/sh

# Run env.sh to create env-config.js with runtime environment variables
/env.sh

# Start nginx
nginx -g "daemon off;" 