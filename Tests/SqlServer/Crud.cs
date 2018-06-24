﻿using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using AdamOneilSoftware;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postulate.Lite.SqlServer;
using Tests.Models;

namespace Tests.SqlServer
{
	[TestClass]
	public class Crud
	{
		private static IDbConnection GetConnection()
		{
			string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
			return new SqlConnection(connectionString);
		}

		private static IDbConnection GetMasterConnection()
		{
			string masterConnection = ConfigurationManager.ConnectionStrings["MasterConnection"].ConnectionString;
			return new SqlConnection(masterConnection);
		}

		[ClassInitialize]
		public static void InitDb(TestContext context)
		{
			try { DropDb(); } catch {  /* do nothing */ }

			using (var cn = GetMasterConnection())
			{
				cn.Execute("CREATE DATABASE [PostulateLite]");				
			}
		}

		[ClassCleanup]
		public static void DropDb()
		{
			using (var cn = GetMasterConnection())
			{
				cn.Execute("DROP DATABASE [PostulateLite]");
			}
		}

		[TestMethod]
		public void DropAndCreateTable()
		{			
			using (var cn = GetConnection())
			{
				DropTable(cn, "Employee");
				cn.CreateTable<Employee>();
			}
		}

		/// <summary>
		/// Drops employee table, creates 10 random employees
		/// </summary>
		[TestMethod]
		public void InsertEmployees()
		{
			using (var cn = GetConnection())
			{
				DropTable(cn, "Employee");
				cn.CreateTable<Employee>();

				var tdg = new TestDataGenerator();
				tdg.Generate<Employee>(10, (record) =>
				{
					record.FirstName = tdg.Random(Source.FirstName);
					record.LastName = tdg.Random(Source.LastName);
					record.Email = $"{record.FirstName}.{record.LastName}@nowhere.org";					
				}, (records) =>
				{
					foreach (var record in records) cn.Save(record);
				});
			}
		}

		[TestMethod]
		public void FindEmployee()
		{
			InsertEmployees();

			using (var cn = GetConnection())
			{
				var e = cn.Find<Employee>(5);
				Assert.IsTrue(e.Id == 5);
			}
		}

		[TestMethod]
		public void UpdateEmployee()
		{
			InsertEmployees();

			const string name = "Django";

			using (var cn = GetConnection())
			{
				var e = cn.Find<Employee>(5);
				e.FirstName = name;
				cn.Save(e);

				e = cn.Find<Employee>(5);
				Assert.IsTrue(e.FirstName.Equals(name));
			}
		}

		[TestMethod]
		public void DeleteEmployee()
		{
			InsertEmployees();

			using (var cn = GetConnection())
			{
				cn.Delete<Employee>(5);
				int count = cn.QuerySingle<int>("SELECT COUNT(1) FROM [dbo].[Employee]");
				Assert.IsTrue(count == 9);
			}
		}

		[TestMethod]
		public void FindWhereEmployee()
		{
			InsertEmployees();

			using (var cn = GetConnection())
			{
				var e = cn.FindWhere(new Employee() { Id = 3 });
				Assert.IsTrue(e.Id == 3);
			}
		}

		private void DropTable(IDbConnection cn, string tableName)
		{
			try
			{
				cn.Execute($"DROP TABLE [{tableName}]");
			}
			catch
			{
				// ignore error
			}
		}
	}
}
