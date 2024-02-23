using LabraryApi.Classes;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reactive.Linq;

namespace LabraryApi.Controllers {
    [ApiController]
    [Route("api/v1/user")]
    public class UserController : ControllerBase {

        private readonly ILogger<UserController> _logger;
        private readonly UserRepository userRepository;

        public UserController(ILogger<UserController> logger, IMongoDatabase mongoDatabase) {
            _logger = logger;
            userRepository=new(mongoDatabase);
        }

        [HttpGet()]
        [Route("getAllUsers")]
        public async Task<IEnumerable<UserDTO>> getAllUsers() {
            _logger.LogInformation("Received request to get all users");
            var users = await userRepository.GetUsers();
            _logger.LogInformation("Retrieved {} users", users.Count());
            return users;
        }
        [HttpGet()]
        [Route("getUserByUsername")]
        public async Task<UserDTO> getUserByUsername(string username) {
            var user = await userRepository.GetUserByIdAsync(username);
            _logger.LogInformation("Retrieved user: {}", user);
            return user;
        }
        [HttpPost()]
        [Route("deleteUserByUsername")]
        public async Task deleteUserByUsername(string username) {
            var user = await userRepository.DeleteUserAsync(username);
            _logger.LogInformation("User deleted successfully with username: {}", username);
        }
        [HttpPost()]
        [Route("createOrUpdate")]
        public async Task<UserDTO> createOrUpdate(UserDTO userDTO) {
            await userRepository.CreateOrUpdateUserAsync(userDTO);
            _logger.LogInformation("User created or updated successfully: {}", userDTO);
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
            await Observable.FromAsync(() => _users.ReplaceOneAsync(e => e.id == user.id, user, new ReplaceOptions { IsUpsert=true }));
            return true;
        }

        public async Task<bool> DeleteUserAsync(string id) {
            await Observable.FromAsync(() => _users.DeleteOneAsync(new BsonDocument(nameof(UserDTO.id), id)));
            return true; 
        }

        public IObservable<UserDTO> GetUserByIdAsync(string userName) {
            return Observable.Create<UserDTO>(async observer =>
            {
                var entities = (await _users.FindAsync(user => user.username == userName)).First();
                observer.OnNext(entities);
                observer.OnCompleted();
            }); 
        }

        public IObservable<UserDTO[]> GetUsers() {
            return Observable.Create<UserDTO[]>(async observer =>
            {
                var entities = (await _users.FindAsync(v => true)).ToList();
                observer.OnNext(entities.ToArray());
                observer.OnCompleted();
            });
        }

    }
}