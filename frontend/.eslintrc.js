module.exports = {
  extends: ["react-app"],
  rules: {
    // Disable exhaustive-deps warnings - these are often not needed in React components
    "react-hooks/exhaustive-deps": "off"
  }
}; 