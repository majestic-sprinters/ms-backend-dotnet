using LabraryApi.Classes;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LabraryApi.Controllers {
    [ApiController]
    [Route("api/v1/book")]
    public class BookController : ControllerBase {

        private readonly ILogger<BookController> _logger;
        private readonly BookRepository bookRepository;

        public BookController(ILogger<BookController> logger, IMongoDatabase mongoDatabase) {
            _logger = logger;
            bookRepository=new(mongoDatabase);
        }

        [HttpGet()]
        [Route("getBooks")]
        public async Task<IEnumerable<BookDTO>> getBooks() {
            _logger.LogInformation("Received request to get all books");
            var books = await bookRepository.GetBooks();
            _logger.LogInformation("Retrieved {} books", books.Count());
            return books;
        }
        [HttpGet()]
        [Route("getBookByName")]
        public async Task<BookDTO> getBookByName(string name) {
            var book = await bookRepository.GetBookByIdAsync(name);
            _logger.LogInformation("Retrieved book: {}", book);
            return book;
        }
        [HttpPost()]
        [Route("deleteBookByName")]
        public async Task deleteBookByName(string name) {
            var book = await bookRepository.DeleteBookAsync(name);
            _logger.LogInformation("Book deleted successfully with name: {}", name);
        }
        [HttpPost()]
        [Route("createOrUpdate")]
        public async Task<BookDTO> createOrUpdate(BookDTO bookDTO) {
            await bookRepository.CreateOrUpdateBookAsync(bookDTO);
            _logger.LogInformation("Book created or updated successfully: {}", bookDTO);
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