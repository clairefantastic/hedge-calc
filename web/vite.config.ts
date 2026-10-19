import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// The dev server forwards /hedges/* to the ASP.NET Core API, so the browser sees one origin
// and the API needs no CORS configuration during development.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/hedges": "http://localhost:5080",
    },
  },
});
