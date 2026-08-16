import { AuthProvider } from "react-oidc-context";

export const oidcConfig = {
    authority : "http://localhost:8080/realms/chrona",
    client_id : "React-SPA",
    redirect_uri : "http://localhost:5173/",
    scope : "openid",
    onSigninCallback: (user) => {
    // Clean code and state from the browser URL
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};