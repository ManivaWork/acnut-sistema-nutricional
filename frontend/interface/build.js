/**
 * Build script para Vercel deployment
 * Substitui {{API_URL}} no index.html pela variável de ambiente VITE_API_URL
 * Uso: node build.js
 * No Vercel: Build Command = "node build.js"
 */
const fs = require('fs');
const path = require('path');

const API_URL = process.env.VITE_API_URL || process.env.API_URL || 'http://localhost:5290/api';

const indexPath = path.join(__dirname, 'index.html');
let html = fs.readFileSync(indexPath, 'utf8');

// Substitui o placeholder
html = html.replace('"{{API_URL}}"', `"${API_URL}"`);

fs.writeFileSync(indexPath, html);

console.log('Build completo!');
console.log('API_URL configurado:', API_URL);