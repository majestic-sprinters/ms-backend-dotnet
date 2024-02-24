using LabraryApi.Classes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Reactive.Linq;

namespace LabraryApi.Controllers {
    [ApiController]
    [Route("api/v1/user")]
    public class UserController : ControllerBase {

        private readonly ILogger<UserController> _logger;
        private readonly UserRepository userRepository;
        private readonly IMemoryCache _cache;

        public UserController(ILogger<UserController> logger, IMongoDatabase mongoDatabase, IMemoryCache cache) {
            _logger = logger;
            userRepository=new(mongoDatabase);
            _cache=cache;
        }
        private async Task<UserDTO[]> getAllUsers() {
            var users = await _cache.GetOrCreateAsync(nameof(getUsers), async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                var users = await userRepository.GetUsers();
                return users;
            });
            return users;
        }
        [HttpGet()]
        [Route("getAllUsers")]
        public async Task<UserDTO[]> getUsers() {
            _logger.LogInformation("Received request to get all users");
            var users = await getAllUsers();
            _logger.LogInformation("Retrieved {} users", users.Count());
            return users;
        }
        [HttpGet()]
        [Route("getUserByUsername")]
        public async Task<UserDTO?> getUserByName(string username) {
            var user = await _cache.GetOrCreateAsync(nameof(getUserByName) + username, async e =>
            {
                var user = await userRepository.GetUserByIdAsync(username);
                return user;
            });
            _logger.LogInformation("Retrieved user: {}", user);
            return user;
        }
        [HttpPost()]
        [Route("deleteUserByUsername")]
        public async Task deleteUserByUsername(string username) {
            var user = await userRepository.DeleteUserAsync(username);
            _cache.Remove(nameof(getUserByName) + username);
            var users = await getAllUsers();
            _cache.Set(nameof(getUsers), users.Where(v => v.username != username).ToArray(), new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            _logger.LogInformation("User deleted successfully with username: {}", username);
        }
        [HttpPost()]
        [Route("createOrUpdate")]
        public async Task<UserDTO> createOrUpdate(UserDTO userDTO) {
            await userRepository.CreateOrUpdateUserAsync(userDTO);
            _cache.Set(nameof(getUserByName) + userDTO.username, userDTO, new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            var users = await getAllUsers();
            if (users.Contains(userDTO)) {
                users = users.Select(v =>
                {
                    if (v.username == userDTO.username) {
                        v = userDTO;
                    }
                    return v;
                }).ToArray()
            ;
            } else {
                users = users.Append(userDTO).ToArray();
            }

            _cache.Set(nameof(getUsers), users, new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            _logger.LogInformation("User created or updated successfully: {}", userDTO.username);
            return userDTO;
        }
    }

    internal interface IuserRepository {
        IObservable<UserDTO[]> GetUsers();
        IObservable<UserDTO> GetUserByIdAsync(string userName);
        Task<bool> CreateOrUpdateUserAsync(UserDTO user);
        Task<bool> DeleteUserAsync(string id);
    }
    internal class UserRepository : IuserRepository {
        private readonly IMongoCollection<UserDTO> _users;

        internal UserRepository(IMongoDatabase database) {
            _users = database.GetCollection<UserDTO>("UserDTO");
        }

        public async Task<bool> CreateOrUpdateUserAsync(UserDTO user) {
            var filter = Builders<UserDTO>.Filter.Eq(p => p.username, user.username);
            var update = Builders<UserDTO>.Update
                .Set(p => p.fio, user.fio)
                .Set(p => p.gender, user.gender);
            var options = new UpdateOptions { IsUpsert = true };

            await Observable.FromAsync(() => _users.UpdateOneAsync(filter, update, options));
            return true;
        }


        public async Task<bool> DeleteUserAsync(string name) {
            var result = await Observable.FromAsync(() => _users.DeleteOneAsync(new BsonDocument(nameof(UserDTO.username), name)));
            return true;
        }

        public IObservable<UserDTO> GetUserByIdAsync(string username) {
            return Observable.Create<UserDTO>(async observer =>
            {

                var entities = (await _users.FindAsync(user => user.username == username)).FirstOrDefault() ?? null;
                observer.OnNext(entities);
                observer.OnCompleted();
            });
        }

        public IObservable<UserDTO[]> GetUsers() {
            return Observable.Create<UserDTO[]>(async observer =>
            {
                var entities = await (await _users.FindAsync(v => true)).ToListAsync();
                observer.OnNext(entities.ToArray());
                observer.OnCompleted();
            });
        }

    }
}