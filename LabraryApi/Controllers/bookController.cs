using LabraryApi.Classes;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LabraryApi.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class bookController : ControllerBase {

        private readonly ILogger<bookController> _logger;
        private readonly BookRepository bookRepository;

        public bookController(ILogger<bookController> logger, IMongoDatabase mongoDatabase) {
            _logger = logger;
            bookRepository=new(mongoDatabase);
        }

        [HttpGet()]
        [Route("getBooks")]
        public async Task<IEnumerable<BookDTO>> getBooks() {
            var books = await bookRepository.GetBooks();
            return books;
        }
        [HttpGet()]
        [Route("getBookByName")]
        public async Task<BookDTO> getBookByName(string name) {
            var book = await bookRepository.GetBookByIdAsync(name);
            return book;
        }
        [HttpPost()]
        [Route("deleteBookByName")]
        public async Task deleteBookByName(string name) {
            var book = await bookRepository.DeleteBookAsync(name);
        }
        [HttpPost()]
        [Route("createOrUpdate")]
        public async Task<BookDTO> createOrUpdate(BookDTO bookDTO) {
            await bookRepository.CreateOrUpdateBookAsync(bookDTO);
            return bookDTO;
        }
    }

    internal interface IbookRepository {
        Task<List<BookDTO>> GetBooks();
        Task<BookDTO> GetBookByIdAsync(string name);
        Task<bool> CreateOrUpdateBookAsync(BookDTO book);
        Task<bool> DeleteBookAsync(string id);
    }
    internal class BookRepository : IbookRepository {
        private readonly IMongoCollection<BookDTO> _books;

        internal BookRepository(IMongoDatabase database) {
            _books = database.GetCollection<BookDTO>("BookDTO");
        }

        public async Task<bool> CreateOrUpdateBookAsync(BookDTO book) {
            await _books.ReplaceOneAsync(e => e.id == book.id, book, new ReplaceOptions { IsUpsert=true });
            return true;
        }

        public async Task<bool> DeleteBookAsync(string id) {
            await _books.DeleteOneAsync(new BsonDocument(nameof(BookDTO.id), id));
            return true;
        }

        public async Task<BookDTO> GetBookByIdAsync(string name) {
            return (await _books.FindAsync(book => book.name == name)).First();
        }

        public async Task<List<BookDTO>> GetBooks() {
            return (await _books.FindAsync(v => true)).ToList();
        }

        // Implement other methods following the async pattern
    }
}