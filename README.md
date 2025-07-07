# OddScout Backend API

Uma API .NET Core robusta para comparação de odds de apostas esportivas, focada em identificar oportunidades de value betting através da análise comparativa entre Pinnacle e Betby.

## 🚀 Tecnologias

- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - API RESTful
- **Entity Framework Core** - ORM para acesso a dados
- **SQL Server** - Banco de dados principal
- **MediatR** - Implementação de CQRS
- **AutoMapper** - Mapeamento objeto-objeto
- **FluentValidation** - Validação de modelos
- **Swagger/OpenAPI** - Documentação da API
- **Playwright** - Web scraping das casas de apostas

## 📁 Estrutura do Projeto

```
src/
├── OddScout.API/              # Camada de apresentação (Controllers)
├── OddScout.Application/      # Lógica de negócio (Commands, Queries, Services)
├── OddScout.Domain/           # Entidades e regras de domínio
└── OddScout.Infrastructure/   # Acesso a dados e serviços externos
```

## 🛠️ Configuração do Ambiente

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB para desenvolvimento)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/)

### Instalação

1. **Clone o repositório**
   ```bash
   git clone [https://github.com/[usuario]/oddscout-backend.git](https://github.com/GustavoAcacioDev/OddScout.git)
   cd oddscout-backend
   ```

2. **Configure a string de conexão**
   ```bash
   # No appsettings.json ou appsettings.Development.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OddScoutDb;Trusted_Connection=true;"
     }
   }
   ```

3. **Execute as migrações**
   ```bash
   dotnet ef database update --project src/OddScout.Infrastructure
   ```

4. **Instale as dependências**
   ```bash
   dotnet restore
   ```

5. **Execute a aplicação**
   ```bash
   dotnet run --project src/OddScout.API
   ```

A API estará disponível em `https://localhost:7001` e a documentação Swagger em `https://localhost:7001/swagger`.

## 🔧 Principais Funcionalidades

### 1. Sistema de Scraping
- Coleta automática de odds da Pinnacle e Betby
- Tratamento de erros e retry logic
- Rate limiting para evitar bloqueios

### 2. Cálculo de Value Betting
- Comparação entre odds de referência (Pinnacle) e oportunidades (Betby)
- Cálculo do valor percentual esperado
- Identificação automática de apostas com valor positivo

### 3. Gerenciamento de Usuários
- Sistema de autenticação JWT
- Perfis de usuário personalizados
- Histórico de apostas e performance

### 4. API RESTful
- Endpoints padronizados seguindo convenções REST
- Documentação automática com Swagger
- Validação robusta de entrada

## 📊 Endpoints Principais

### Autenticação
- `POST /api/auth/register` - Registro de usuário
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Renovar token

### Value Bets
- `GET /api/valuebets` - Listar value bets
- `GET /api/valuebets/{id}` - Detalhes de um value bet
- `POST /api/valuebets/calculate` - Calcular value bets

### Eventos
- `GET /api/events` - Listar eventos esportivos
- `GET /api/events/{id}/odds` - Odds de um evento específico

## 🤝 Contribuição

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

### Padrões de Commit
- `feat:` Nova funcionalidade
- `fix:` Correção de bug
- `docs:` Documentação
- `style:` Formatação
- `refactor:` Refatoração de código
- `test:` Adição de testes


## 👥 Time

- **Desenvolvedor**: Gustavo Ferreira

---
