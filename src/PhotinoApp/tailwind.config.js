/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./wwwroot/**/*.{html,js}",
        "../PhotinoApp.Razor/**/*.{html,js,razor}"
    ],
  theme: {
    extend: {},
  },
  plugins: [
    require("@tailwindcss/typography"),
    require("daisyui")
  ],
}