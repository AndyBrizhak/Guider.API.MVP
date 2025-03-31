# Guider API MVP

## Описание проекта

Guider API MVP - это API для работы с коллекцией мест (Places) в базе данных MongoDB. API предоставляет возможности для получения, создания, обновления и удаления документов, а также для выполнения гео-поиска с использованием различных фильтров.

## Функциональные возможности

- Получение всех документов из коллекции Places
- Получение документа по ID
- Получение документа по веб-параметру
- Получение документа по ID из заголовка
- Гео-поиск ближайших мест
- Гео-поиск ближайших мест с центром и лимитом
- Гео-поиск по категории и тегам
- Гео-поиск с текстовым поиском по всем текстовым полям

## Установка и настройка

1. Клонируйте репозиторий:
   

```
   git clone https://gitlab.com/yourusername/guider-api-mvp.git
   

```

2. Перейдите в директорию проекта:
   

```
   cd guider-api-mvp
   

```

3. Настройте параметры подключения к MongoDB в файле `appsettings.Development.json`:
   

```
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "MongoDBSettings": {
       "ConnectionString": "*******",
       "DatabaseName": "guider",
       "CollectionName": "places_clear"
     }
   }
   

```

4. Запустите проект:
   

```
   dotnet run
   

```

## Использование

### Получение всех документов



```
GET /api/place


```

### Получение документа по ID



```
GET /api/place/{id}


```

### Получение документа по веб-параметру



```
GET /api/place/search/{web}


```

### Получение документа по ID из заголовка



```
GET /api/place/id?web={web}&id={id}


```

### Гео-поиск ближайших мест



```
GET /api/place/geonear
Headers:
  X-Latitude: {lat}
  X-Longitude: {lng}
  X-Max-Distance: {maxDistance}


```

### Гео-поиск ближайших мест с центром и лимитом



```
GET /api/place/geonearlimit
Headers:
  X-Latitude: {lat}
  X-Longitude: {lng}
  X-Max-Distance: {radiusMeters}
  X-Limit: {limit}


```

### Гео-поиск по категории и тегам



```
GET /api/place/geo/category/tags?lat={lat}&lng={lng}&maxDistanceMeters={maxDistanceMeters}&category={category}&filterTags={filterTags}


```

### Гео-поиск с текстовым поиском



```
GET /api/place/geonear/search?lat={lat}&lng={lng}&maxDistanceMeters={maxDistanceMeters}&limit={limit}&searchText={searchText}


```

## Лицензия

Этот проект лицензирован под лицензией MIT. Подробности см. в файле LICENSE.