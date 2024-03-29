using LabraryApi.Classes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reactive.Linq;


namespace LabraryApi.Controllers {
    [ApiController]
    [Route("api/v1/book")]
    public class BookController : ControllerBase {

        private readonly ILogger<BookController> _logger;
        private readonly BookRepository bookRepository;
        private readonly IMemoryCache _cache;

        public BookController(ILogger<BookController> logger, IMongoDatabase mongoDatabase, IMemoryCache cache) {
            _logger = logger;
            bookRepository=new(mongoDatabase);
            _cache=cache;
        }
        private async Task<BookDTO[]> getAllBooks() {
            var books = await _cache.GetOrCreateAsync(nameof(getBooks), async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                var books = await bookRepository.GetBooks();
                return books;
            });
            return books;
        }
        [HttpGet()]
        [Route("getBooks")]
        public async Task<BookDTO[]> getBooks() {
            _logger.LogInformation("Received request to get all books");
            var books = await getAllBooks();
            _logger.LogInformation("Retrieved {} books", books.Count());
            return books;
        }
        [HttpGet()]
        [Route("getBookByName")]
        public async Task<BookDTO?> getBookByName(string name) {
            var book = await _cache.GetOrCreateAsync(nameof(getBookByName) + name, async e =>
            {
                var book = await bookRepository.GetBookByIdAsync(name);
                return book;
            });
            _logger.LogInformation("Retrieved book: {}", book);
            return book;
        }
        [HttpPost()]
        [Route("deleteBookByName")]
        public async Task deleteBookByName(string name) {
            var book = await bookRepository.DeleteBookAsync(name);
            _cache.Remove(nameof(getBookByName) + name);
            var books = await getAllBooks();
            _cache.Set(nameof(getBooks), books.Where(v => v.name != name).ToArray(), new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            _logger.LogInformation("Book deleted successfully with name: {}", name);
        }
        [HttpPost()]
        [Route("createOrUpdate")]
        public async Task<BookDTO> createOrUpdate(BookDTO bookDTO) {
            await bookRepository.CreateOrUpdateBookAsync(bookDTO);
            _cache.Set(nameof(getBookByName) + bookDTO.name, bookDTO, new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            var books = await getAllBooks();
            if (books.Contains(bookDTO)) {
                books = books.Select(v =>
                {
                    if (v.name == bookDTO.name) {
                        v = bookDTO;
                    }
                    return v;
                }).ToArray()
            ;
            } else {
                books = books.Append(bookDTO).ToArray();
            }
            _cache.Set(nameof(getBooks), books, new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            _logger.LogInformation("Book created or updated successfully: {}", bookDTO.name);
            return bookDTO;
        }
    }

    internal interface IbookRepository {
        IObservable<BookDTO[]> GetBooks();
        IObservable<BookDTO> GetBookByIdAsync(string name);
        Task<bool> CreateOrUpdateBookAsync(BookDTO book);
        Task<bool> DeleteBookAsync(string id);
    }
    internal class BookRepository : IbookRepository {
        private readonly IMongoCollection<BookDTO> _books;

        internal BookRepository(IMongoDatabase database) {
            _books = database.GetCollection<BookDTO>("BookDTO");
        }

        public async Task<bool> CreateOrUpdateBookAsync(BookDTO book) {
            var filter = Builders<BookDTO>.Filter.Eq(p => p.name, book.name);
            var update = Builders<BookDTO>.Update
                .Set(p => p.description, book.description)
                .Set(p => p.author, book.author)
                .Set(p => p.year, book.year)
                .Set(p => p.publisher, book.publisher);
            var options = new UpdateOptions { IsUpsert = true };

            await Observable.FromAsync(() => _books.UpdateOneAsync(filter, update, options));
            return true;
        }

 
        public async Task<bool> DeleteBookAsync(string name) {
            var result = await Observable.FromAsync(() => _books.DeleteOneAsync(new BsonDocument(nameof(BookDTO.name), name)));
            return true;
        }

        public IObservable<BookDTO> GetBookByIdAsync(string name) {
            return Observable.Create<BookDTO>(async observer =>
            {

                var entities = (await _books.FindAsync(book => book.name == name)).FirstOrDefault() ?? null;
                observer.OnNext(entities);
                observer.OnCompleted();
            });
        }

        public IObservable<BookDTO[]> GetBooks() {
            return Observable.Create<BookDTO[]>(async observer =>
            {
                var entities = await (await _books.FindAsync(v => true)).ToListAsync();
                observer.OnNext(entities.ToArray());
                observer.OnCompleted();
            });
        }

        // Implement other methods following the async pattern
    }
}
