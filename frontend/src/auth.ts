export const oidcConfig = {
  authority: "http://localhost:8080/realms/chrona",
  client_id: "React-SPA",
  redirect_uri: "http://localhost:5173/",
  post_logout_redirect_uri: "http://localhost:5173/",
  scope: "openid",
  onSigninCallback: () => {
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};