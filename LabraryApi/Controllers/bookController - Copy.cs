using LabraryApi.Classes;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LabraryApi.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class userController : ControllerBase {

        private readonly ILogger<userController> _logger;
        private readonly UserRepository userRepository;

        public userController(ILogger<userController> logger, IMongoDatabase mongoDatabase) {
            _logger = logger;
            userRepository=new(mongoDatabase);
        }

        [HttpGet()]
        [Route("getAllUsers")]
        public async Task<IEnumerable<UserDTO>> getAllUsers() {
            var users = await userRepository.GetUsers();
            return users;
        }
        [HttpGet()]
        [Route("getUserByUsername")]
        public async Task<UserDTO> getUserByUsername(string username) {
            var user = await userRepository.GetUserByIdAsync(username);
            return user;
        }
        [HttpPost()]
        [Route("deleteUserByUsername")]
        public async Task deleteUserByUsername(string username) {
            var user = await userRepository.DeleteUserAsync(username);
        }
        [HttpPost()]
        [Route("createOrUpdate")]
        public async Task<UserDTO> createOrUpdate(UserDTO userDTO) {
            await userRepository.CreateOrUpdateUserAsync(userDTO);
            return userDTO;
        }
    }

    internal interface IuserRepository {
        Task<List<UserDTO>> GetUsers();
        Task<UserDTO> GetUserByIdAsync(string userName);
        Task<bool> CreateOrUpdateUserAsync(UserDTO user);
        Task<bool> DeleteUserAsync(string id);
    }
    internal class UserRepository : IuserRepository {
        private readonly IMongoCollection<UserDTO> _users;

        internal UserRepository(IMongoDatabase database) {
            _users = database.GetCollection<UserDTO>("UserDTO");
        }

        public async Task<bool> CreateOrUpdateUserAsync(UserDTO user) {
            await _users.ReplaceOneAsync(e => e.id == user.id, user, new ReplaceOptions { IsUpsert=true });
            return true;
        }

        public async Task<bool> DeleteUserAsync(string id) {
            await _users.DeleteOneAsync(new BsonDocument(nameof(UserDTO.id), id));
            return true;
        }

        public async Task<UserDTO> GetUserByIdAsync(string userName) {
            return (await _users.FindAsync(user => user.username == userName)).First();
        }

        public async Task<List<UserDTO>> GetUsers() {
            return (await _users.FindAsync(v => true)).ToList();
        }

    }
}