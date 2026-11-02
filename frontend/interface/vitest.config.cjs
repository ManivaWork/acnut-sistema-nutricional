// vitest.config.cjs - CommonJS para compatibilidade
const { defineConfig } = require('vitest/config')

module.exports = defineConfig({
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./tests/setup.js'],
    include: ['tests/**/*.test.js'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: ['node_modules/', 'tests/', '*.config.js']
    }
  }
})