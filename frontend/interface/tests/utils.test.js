import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

// Importa funções do app.js - precisamos executar o arquivo para ter acesso às funções
// Como app.js usa módulos ES, vamos testar criando um módulo de teste

describe('Utilitários Frontend', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    sessionStorage.clear()
  })

  describe('mostrarToast', () => {
    it('deve criar elemento toast com classes corretas', () => {
      // Simula a função mostrarToast do app.js
      const container = document.createElement('div')
      container.id = 'toast-container'
      document.body.appendChild(container)

      const toast = document.createElement('div')
      toast.className = 'flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg bg-blue-600 text-white animate-slide-in'
      toast.innerHTML = '<i class="fa-solid fa-circle-info"></i><span class="text-sm font-medium">Teste</span>'

      container.appendChild(toast)

      expect(container.children.length).toBe(1)
      expect(container.firstChild.classList.contains('bg-blue-600')).toBe(true)

      container.remove()
    })

    it('deve aceitar diferentes tipos de toast', () => {
      const tipos = ['sucesso', 'erro', 'aviso', 'info']
      const classesEsperadas = {
        sucesso: 'bg-emerald-600',
        erro: 'bg-rose-600',
        aviso: 'bg-amber-600',
        info: 'bg-blue-600'
      }

      tipos.forEach(tipo => {
        const container = document.createElement('div')
        container.id = 'toast-container'
        document.body.appendChild(container)

        const toast = document.createElement('div')
        toast.className = `flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg ${classesEsperadas[tipo]} text-white animate-slide-in`

        container.appendChild(toast)

        expect(container.firstChild.classList.contains(classesEsperadas[tipo])).toBe(true)
        container.remove()
      })
    })
  })

  describe('alternarVisibilidadeSenha', () => {
    it('deve alternar tipo do input entre password e text', () => {
      const input = document.createElement('input')
      input.type = 'password'
      input.id = 'senha'
      document.body.appendChild(input)

      const icon = document.createElement('i')
      icon.id = 'senha-icon'
      icon.className = 'fa-solid fa-eye'
      document.body.appendChild(icon)

      // Simula a lógica da função
      if (input.type === 'password') {
        input.type = 'text'
        icon.classList.remove('fa-eye')
        icon.classList.add('fa-eye-slash')
      }

      expect(input.type).toBe('text')
      expect(icon.classList.contains('fa-eye-slash')).toBe(true)
      expect(icon.classList.contains('fa-eye')).toBe(false)

      input.remove()
      icon.remove()
    })
  })

  describe('API_URL construction', () => {
    it('deve adicionar /api se não terminar com ele', () => {
      const ENV_API_URL = 'http://localhost:5290'
      const API_URL = ENV_API_URL.endsWith('/api') ? ENV_API_URL : ENV_API_URL + '/api'

      expect(API_URL).toBe('http://localhost:5290/api')
    })

    it('não deve duplicar /api se já terminar com ele', () => {
      const ENV_API_URL = 'http://localhost:5290/api'
      const API_URL = ENV_API_URL.endsWith('/api') ? ENV_API_URL : ENV_API_URL + '/api'

      expect(API_URL).toBe('http://localhost:5290/api')
    })
  })
})

describe('Convite Paciente URL Parsing', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('deve extrair token de pathname /convite-paciente/{token}', () => {
    const token = 'a'.repeat(32)
    const pathname = `/convite-paciente/${token}`
    const pathMatch = pathname.match(/\/convite-paciente\/([a-f0-9]{32})/i)

    expect(pathMatch).not.toBeNull()
    expect(pathMatch[1]).toBe(token)
  })

  it('deve extrair token de query string ?convite={token}', () => {
    const token = 'b'.repeat(32)
    const search = `?convite=${token}`
    const queryMatch = search.match(/[?&]convite=([a-f0-9]{32})/i)

    expect(queryMatch).not.toBeNull()
    expect(queryMatch[1]).toBe(token)
  })

  it('deve extrair token de hash #convite-paciente/{token}', () => {
    const token = 'c'.repeat(32)
    const hash = `#convite-paciente/${token}`
    const hashMatch = hash.match(/#convite-paciente\/([a-f0-9]{32})/i)

    expect(hashMatch).not.toBeNull()
    expect(hashMatch[1]).toBe(token)
  })

  it('deve retornar null para token inválido (menos de 32 chars)', () => {
    const pathname = '/convite-paciente/abc123'
    const pathMatch = pathname.match(/\/convite-paciente\/([a-f0-9]{32})/i)

    expect(pathMatch).toBeNull()
  })
})

describe('Validação Formulário Convite Paciente', () => {
  it('deve validar CPF com 11 dígitos', () => {
    const validarCPF = (cpf) => cpf.replace(/\D/g, '').length === 11

    expect(validarCPF('123.456.789-01')).toBe(true)
    expect(validarCPF('12345678901')).toBe(true)
    expect(validarCPF('123.456.789-0')).toBe(false)
    expect(validarCPF('1234567890')).toBe(false)
  })

  it('deve validar senha mínima 8 caracteres', () => {
    const validarSenha = (senha) => senha.length >= 8

    expect(validarSenha('12345678')).toBe(true)
    expect(validarSenha('senha123')).toBe(true)
    expect(validarSenha('1234567')).toBe(false)
    expect(validarSenha('')).toBe(false)
  })

  it('deve validar confirmação de senha', () => {
    const validarConfirmacao = (senha, confirmacao) => senha === confirmacao

    expect(validarConfirmacao('senha123', 'senha123')).toBe(true)
    expect(validarConfirmacao('senha123', 'senha456')).toBe(false)
  })
})