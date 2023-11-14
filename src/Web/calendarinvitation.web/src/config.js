export function getConfig() {
  var configJson = require('./auth_config.json');

  return {
    domain: configJson.domain,
    clientId: configJson.clientId,
    audience: configJson.audience,
    apiOrigin: configJson.apiOrigin
  };
}
