# WeatherSystem — ЛР7 (Render + PostgreSQL)

Система моніторингу погоди: REST API на ASP.NET Core 8 (WeatherSystem.API), база PostgreSQL через EF Core,
клієнтська вебсторінка (WeatherSystem.Web) і віконний клієнт (WeatherSystem.Desktop).

- API: https://weather-api-romanova.onrender.com/swagger
- Перевірка стану: https://weather-api-romanova.onrender.com/health
- Вебсторінка: https://weather-web-romanova.onrender.com

Розгортання описане у файлі `render.yaml`. Рядок підключення до бази передається змінною
середовища `ConnectionStrings__DefaultConnection` і в репозиторії не зберігається.
