'use strict';

// Generate a bcrypt hash for a password so you can paste it into STUDIO_USERS.
// Usage:  node scripts/hash.js "the-password"
//   or:   npm run hash -- "the-password"

const bcrypt = require('bcryptjs');

const pw = process.argv[2];
if (!pw) {
  console.error('Usage: node scripts/hash.js "<password>"');
  process.exit(1);
}

const hash = bcrypt.hashSync(pw, 12);
console.log(hash);
