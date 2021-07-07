using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.DBClass
{
    public class DBManager
    {
        private static readonly string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=usfvastorage;AccountKey=CHewJcFpleROMCkgCNTgFdT3y2OUet4n7yDaDhBXxCNvkLBYjpH8QC0gYQcYFiLhq9vqEpVe3NLEzzzsvkb/6g==;EndpointSuffix=core.windows.net";
        private static CloudTableClient cloudTableClient = CloudStorageAccount.Parse(ConnectionString).CreateCloudTableClient(new TableClientConfiguration());
        private static readonly string tableName = "Items";
        public static CloudTable entityTable = cloudTableClient.GetTableReference(tableName);
        public static CloudTable relationTable = cloudTableClient.GetTableReference("relation");
        public static List<string> Categories = new List<string>();
        public DBManager()
        {
            InitDatabaseAsync().Wait();
            if (Categories.Count() == 0) PopulateCategories();
        }

        //Initialize the database if it does not exist
        private async Task InitDatabaseAsync()
        {
            try
            {
                entityTable?.Exists();
            }
            catch (Exception)
            {
                try
                {
                    cloudTableClient = CloudStorageAccount.Parse(ConnectionString).CreateCloudTableClient(new TableClientConfiguration());
                    entityTable = cloudTableClient.GetTableReference(tableName);
                    await entityTable.CreateIfNotExistsAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: failed to create {0}", e.Message);
                }
            }
            try
            {
                relationTable?.Exists();
            }
            catch (Exception)
            {
                try
                {
                    cloudTableClient = CloudStorageAccount.Parse(ConnectionString).CreateCloudTableClient(new TableClientConfiguration());
                    relationTable = cloudTableClient.GetTableReference("relation");
                    await relationTable.CreateIfNotExistsAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: failed to create {0}", e.Message);
                }
            }
        }

        //  Populate the category list for use in conversation with the user or other areas
        private void PopulateCategories() => Categories = entityTable.CreateQuery<AdviceEntity>()
                    .Where(x => x.PartitionKey == "AdviceEntity")
                    .Select(x => new string(x.Category))
                    .AsEnumerable().Distinct().ToList();

        //  Add an item to the database, agnostic of type
        public async Task<T> AddItemAsync<T>(T entity) where T : TableEntity
        {
            try
            {
                TableResult result = await entityTable.ExecuteAsync(TableOperation.InsertOrMerge(entity));
                T addedEntity = result.Result as T;
                Console.WriteLine("Info: wrote {0}", result.Result.ToString());
                if (addedEntity.PartitionKey == "AdviceEntity" )
                {
                    var advice = addedEntity as AdviceEntity; 
                    if (!Categories.Contains(advice.Category)) Categories.Add(advice.Category);
                }
                return addedEntity;
            }
            catch (StorageException e)
            {
                Console.WriteLine("Error: failed to add {0}", e.Message);
                throw;
            }
        }

        //  Add items to the database, agnostic of type in the form of a list
        public async void AddItemsBatchAsync<T>(List<T> entities) where T : TableEntity
        {
            try
            {
                TableBatchOperation tableOperations = new TableBatchOperation();
                entities.ForEach(x => tableOperations.Add(TableOperation.InsertOrMerge(x)));
                TableBatchResult result = await entityTable.ExecuteBatchAsync(tableOperations);
                if (entities.FirstOrDefault().PartitionKey == "AdviceEntity")
                {
                    Categories.Union((entities as List<AdviceEntity>).Select(x => x.Category));
                }
            }
            catch (StorageException e)
            {
                Console.WriteLine("Error: failed to batch add {0}", e.Message);
                throw;
            }
        }

        //  Get an item from the database, agnostic of type
        public async Task<T> GetItemAsync<T>(string typeKey, string id) where T : TableEntity
        {
            try
            {
                Console.WriteLine($"Debug: {typeKey}, {id}");
                TableResult result = await entityTable.ExecuteAsync(TableOperation.Retrieve<T>(typeKey, id));
                return result.Result as T;
            }
            catch (StorageException e)
            {
                Console.WriteLine("Error: failed to get {0}", e.Message);
                throw;
            }
        }

        //  Remove an item from the database, agnostic of type
        public async Task RemoveItemAsync<T>(T entity) where T : TableEntity
        {
            try
            {
                _ = await entityTable.ExecuteAsync(TableOperation.Delete(entity));
            }
            catch (StorageException e)
            {
                Console.WriteLine("Error: failed to remove {0}", e.Message);
                throw;
            }
        }

        //  Add to the relation table advice seen/given to a user
        public async Task AddRelationAsync(AdviceConnection relation)
        {
            try
            {
                _ = await relationTable.ExecuteAsync(TableOperation.InsertOrMerge(relation));
            }
            catch(StorageException e)
            {
                Console.WriteLine("Error: failed to add {0}", e.Message);
                throw;
            }
        }

        //  Remove a user's category of seen advice
        public async Task RemoveRelationsAsync(string UserId, string Category)
        {
            try
            {
                List<AdviceConnection> query = relationTable.CreateQuery<AdviceConnection>()
                    .Where(x => x.PartitionKey.Equals(UserId) && x.Category.Equals(Category))
                    .AsEnumerable().ToList();
                TableBatchOperation removeOperation = new TableBatchOperation();
                query.ForEach(x => removeOperation.Add(TableOperation.Delete(x)));
                await relationTable.ExecuteBatchAsync(removeOperation);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: failed to batch delete relations {0}", e.Message);
                throw;
            }
        }

        //  Check for existence of entities
        public bool TestSubscriptionIdExists(string id) => entityTable.CreateQuery<SubscriptionEntity>().Where(x => x.PartitionKey == "SubscriptionEntity" && x.RowKey == id).AsEnumerable().Any();
        public bool TestAdviceIdExists(string id) => entityTable.CreateQuery<AdviceEntity>().Where(x => x.PartitionKey == "AdviceEntity" && x.RowKey == id).AsEnumerable().Any();
        public bool TestUserExists(string email) => entityTable.CreateQuery<UserEntity>().Where(x => x.PartitionKey == "UserEntity" && x.RowKey == email).AsEnumerable().Any();

        //  Update category list used locally with new category
        public static string AddCategoryToList(string category) 
        {
            //format string, capitalize each word (title case)
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            var formattedString = textInfo.ToTitleCase(category.ToLower());
            Categories.Add(formattedString);
            return formattedString;
        }

        //  Remove category list used locally with new category
        public static string RemoveCategoryFromList(string category)
        {
            //format string, capitalize each word (title case)
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            var formattedString = textInfo.ToTitleCase(category.ToLower());
            Categories.Remove(formattedString);
            return formattedString;
        }

        //  Initialize an empty database with categorized advice from the QNA-KB excel sheet.
/*        private static string previousQNAID;
        // Initializing the assistant's database with advice.
        private async Task PopulateDBwithAdvice()
        {
            using (var package = new ExcelPackage(new FileInfo("QNA-KB.xlsx")))
            {
                var firstSheet = package.Workbook.Worksheets["Kb-Exported-Content"];

                const int startHeaderRow = 2;
                int maxRow = firstSheet.Dimension.End.Row;

                const char adviceCol = 'B';
                const char categoryCol = 'D';
                const char QNAIDCol = 'H';

                var sortRange = firstSheet.Cells[string.Format("{0}{1}:{2}{3}", adviceCol, startHeaderRow+1, QNAIDCol, maxRow)];
#if DEBUG
                int temp = QNAIDCol - adviceCol + 1;
#endif //DEBUG
                sortRange.Sort(QNAIDCol - adviceCol, false);

                var existingQNAID = entityTable.CreateQuery<AdviceEntity>()
                        .Where(x => x.PartitionKey.Equals("AdviceEntity") && x.RowKey.Contains("-"))
                        .Select(x => x.RowKey)
                        .AsEnumerable().ToList();
                List<AdviceEntity> adviceEntities = new List<AdviceEntity>();

                for (int i = startHeaderRow + 1; i <= maxRow; i++) 
                {
#if DEBUG
                    var cellAnswer = firstSheet.Cells[adviceCol + i.ToString()];
                    var cellMetadata = firstSheet.Cells[categoryCol + i.ToString()];
#endif //DEBUG
                    var advice = firstSheet.Cells[adviceCol + i.ToString()].Text;
                    var categories = firstSheet.Cells[categoryCol + i.ToString()].Text.Split(';');
                    var qnaId = firstSheet.Cells[QNAIDCol + i.ToString()].Text;

                    if (advice == "" || qnaId == "" || categories.Length == 0 || previousQNAID == qnaId || categories[0] == "" ) { continue; }

                    int j = 0;
                    if (!existingQNAID.Contains(qnaId+"-"+j.ToString()))
                    {
                        foreach (var cat in categories)
                        {
                            if (cat == null || cat == "") { continue; }
                            adviceEntities.Add(new AdviceEntity(qnaId + "-" + j.ToString(), cat, advice));
                            if (adviceEntities.Count >= 100)
                            {
                                AddItemsBatchAsync(adviceEntities);
                                adviceEntities.Clear();
                            }
                            previousQNAID = qnaId;
                            j++;
                        }
                    }
                }
                if ( adviceEntities.Any() ) AddItemsBatchAsync(adviceEntities);
            }
        }*/
    }
}
