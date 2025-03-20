namespace Guider.API.MVP.Services
{
    using Guider.API.MVP.Models;
    using MongoDB.Driver;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public class PlaceService
    {
        private readonly IMongoCollection<Place> _placeCollection;

        public PlaceService(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("guider"); // Замените на реальное имя базы
            _placeCollection = database.GetCollection<Place>("places_with_location");
        }

        public async Task<List<Place>> GetAllAsync() =>
            await _placeCollection.Find(_ => true).ToListAsync();

        public async Task<Place?> GetByIdAsync(string id) =>
            await _placeCollection.Find(b => b.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Place place) =>
            await _placeCollection.InsertOneAsync(place);

        public async Task UpdateAsync(string id, Place updatedPlace) =>
            await _placeCollection.ReplaceOneAsync(b => b.Id == id, updatedPlace);

        public async Task DeleteAsync(string id) =>
            await _placeCollection.DeleteOneAsync(b => b.Id == id);
    }
}
