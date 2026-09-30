// Dev-server proxy for the Portuguese build (npm run start:pt): the same /api proxy, without
// the /pt forward — this server IS /pt, so forwarding it would loop back to itself.
import config from './proxy.conf.mjs';

const { '/pt': _, ...portugueseConfig } = config;

export default portugueseConfig;
