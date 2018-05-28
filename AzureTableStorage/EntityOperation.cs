using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace SimpleEchoBot.AzureTableStorage
{
    [Serializable]
    public class EntityOperation<E> where E : TableEntity
    {
        #region Entity挿入
        /// <summary>
        /// Entity挿入
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public TableResult InsertEntityResult(E entity, string tableName)
        {
            //エンティティを用いて挿入用命令を作成
            TableOperation insertOperation = TableOperation.Insert(entity);

            //指定テーブルに挿入命令を実行し、結果を返す
            return StorageOperation.GetTableIfNotExistsCreate(tableName).Execute(insertOperation);

        }
        #endregion

        #region Entity検索
        /// <summary>
        /// Entity検索
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public TableResult RetrieveEntityResult(string partitionKey, string rowKey, string tableName)
        {
            //検索操作を行う変数を生成
            TableOperation retrieveOperation = TableOperation.Retrieve<E>(partitionKey, rowKey);

            //テーブルに対して検索操作を行い、結果を返す
            return StorageOperation.GetTableIfNotExistsCreate(tableName).Execute(retrieveOperation);
        }
        #endregion

        #region Entity更新
        /// <summary>
        /// Entity更新
        /// </summary>
        /// <returns></returns>
        public TableResult UpdateEntityResult(E entity, string tableName)
        {
            //アップデート対象のエンティティを取得
            E updateEntity = RetrieveEntityResult(entity.PartitionKey, entity.RowKey, tableName).Result as E;

            //アップデート対象のエンティティを指定
            entity.PartitionKey = updateEntity.PartitionKey;
            entity.RowKey = updateEntity.RowKey;
            entity.ETag = updateEntity.ETag;

            //更新操作を行う変数を生成
            TableOperation replaceOperation = TableOperation.Replace(entity);

            //テーブルに対して更新操作を行い、結果を返す
            return StorageOperation.GetTableIfNotExistsCreate(tableName).Execute(replaceOperation);
        }
        #endregion

        #region Entity挿入か更新
        /// <summary>
        /// Entity挿入か更新
        /// </summary>
        /// <returns></returns>
        public TableResult InsertOrUpdateEntityResult(E entity, string tableName)
        {
            //挿入or更新操作を行う変数を生成
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

            //テーブルに対して挿入or更新操作を行う
            return StorageOperation.GetTableIfNotExistsCreate(tableName).Execute(insertOrReplaceOperation);
        }
        #endregion
    }
}