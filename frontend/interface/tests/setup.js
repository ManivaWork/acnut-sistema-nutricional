import { vi } from 'vitest'

// Mock DOM APIs
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn()
}))

// Mock sessionStorage using Object.defineProperty
const sessionStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn()
}
Object.defineProperty(global, 'sessionStorage', {
  value: sessionStorageMock,
  writable: true,
  configurable: true
})

// Mock fetch
global.fetch = vi.fn()

// Mock URL.createObjectURL / revokeObjectURL
global.URL.createObjectURL = vi.fn(() => 'blob:mock')
global.URL.revokeObjectURL = vi.fn()

// Mock window.location
Object.defineProperty(window, 'location', {
  value: {
    hostname: 'localhost',
    pathname: '/',
    search: '',
    hash: '',
    origin: 'http://localhost:5500'
  },
  writable: true,
  configurable: true
})

// Mock window.history
Object.defineProperty(window, 'history', {
  value: {
    replaceState: vi.fn(),
    pushState: vi.fn()
  },
  writable: true,
  configurable: true
})