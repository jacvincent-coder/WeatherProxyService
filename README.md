# WeatherProxyService

## 📘 Overview
WeatherProxyService is a lightweight full‑stack application consisting of:

- **.NET 8 Web API (Backend)** acting as a proxy for OpenWeather’s API  
- **React + TypeScript (Frontend)** to input city, country and display weather description  
- **API Key validation**, **Rate limiting**, and **Unit tests with coverage**

---

# 🚀 Backend (API) – Build / Run / Test

## ✔ Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code

## ✔ Build & Run the API
```bash
cd WeatherProxyService
dotnet run
```

Swagger UI will be available at:

```
https://localhost:5001/swagger
```

### Required Request Header
Your API requests must include:

```
X-Api-Key: client-key-1
```

## ✔ Run Backend Tests
```bash
cd WeatherProxyService.Tests
dotnet test
```

### Test Coverage (optional)
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutput=coverage/ /p:CoverletOutputFormat=cobertura
reportgenerator -reports:coverage/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
```

Coverage report:
```
WeatherProxyService.Tests/coveragereport/index.html
```

---

# 🌤️ Frontend (React + TypeScript) – Build / Run

## ✔ Prerequisites
- Node.js (LTS)
- npm

## ✔ Install Dependencies
```bash
cd weather-proxy-ui
npm install
```

## ✔ Run the Development Server
```bash
npm start
```

App runs at:
```
http://localhost:3000
```

The React app is configured with:
```json
"proxy": "https://localhost:5001"
```
So you can call `/api/weather` directly during development.

---

# 🛠 How It Works (Short Summary)
- The backend exposes a single endpoint:  
  `GET /api/weather?city={city}&country={country}`
- Backend validates:
  - API key  
  - Rate limit (5 req/hour per client)
- Backend forwards the request to OpenWeather using rotating API keys
- Only returns the **weather description** field
- Frontend sends requests and displays:
  - Weather description  
  - Errors  
  - Rate limit header details

---

# 🌱 Future Enhancements
- Add strict city–country validation using Geocoding API  
- Add Application Insights telemetry  
- Add Polly (Retry / Timeout / Circuit Breaker)  
- Add Docker support  
- Enhance UI with TailwindCSS or Material UI  
- Add caching for repeated weather lookups  

---

# 📬 Author
Jacob Vincent
