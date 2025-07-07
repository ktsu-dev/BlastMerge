using ktsu.FileSystemProvider;
using ktsu.PersistenceProvider;
using ktsu.SerializationProvider;
using ktsu.UniversalSerializer;

namespace TestAPI
{
    public class ApiExplorer
    {
        public void ExploreFileSystemProvider()
        {
            // Let's see what's available in IFileSystemProvider
            IFileSystemProvider? provider = null;
            
            // Try to find available methods and properties
            // provider.
        }
        
        public void ExplorePersistenceProvider()
        {
            // Let's try different ways to create a PersistenceProvider
            // Maybe it's just PersistenceProvider (not generic)
            // var persistence = new PersistenceProvider();
            
            // Or maybe there's a different class name
            // var persistence = new FilePersistenceProvider();
            
            // Let's see what's actually available
            // IPersistenceProvider persistence = null;
        }
        
        public void ExploreSerializationProvider()
        {
            // Let's try different ways to create a SerializationProvider
            // var serialization = new SerializationProvider();
            
            // Or maybe there's a different class name
            // var serialization = new JsonSerializationProvider();
            
            // Let's see what's actually available
            // ISerializationProvider serialization = null;
        }
    }
} 