# 🚀 Hash Processing System - Full Async .NET 8, RabbitMQ, MariaDB

This project demonstrates a scalable distributed system with asynchronous communication between services.  
It uses RabbitMQ for message queuing and MariaDB for persistence, all implemented using modern .NET 8.0 practices.

---

## 🧩 Project Structure

/Shared --> Shared models, configurations, utilities 
/MessageProcessingSystemAPI --> REST API to generate and send SHA1 hashes 
/MessageProcessingSystemProcessor --> Background worker to consume and save hashes


---

## 📦 Technology Stack

- **C# / .NET 8.0**
- **RabbitMQ.Client 8.x** (fully async)
- **MariaDB**
- **Dapper ORM**
- **Docker** (for RabbitMQ)
- **RabbitMQ Web Management Plugin**

---

## 🏗 How the System Works

1. **MessageProcessingSystemAPI**
   - Provides a `POST /hashes` endpoint.
   - Generates 40,000 random SHA1 hashes in 4 parallel tasks (10,000 per task).
   - Publishes hashes to a RabbitMQ queue asynchronously.

2. **MessageProcessingSystemProcessor**
   - Consumes messages asynchronously from RabbitMQ using 4 threads.
   - Each message (a SHA1 hash) is inserted into MariaDB.
   - If saving fails, the message is negatively acknowledged without requeue.

3. **Shared**
   - Defines models (DTOs) and configuration classes for consistency.

---

## 🛠 How to Set Up and Run

### 1. Start RabbitMQ (Docker)

Run this command:

bash
docker run -d --hostname rmq --name rabbit-hornet -p 8080:15672 -p 5672:5672 rabbitmq:3-management

Access RabbitMQ UI: http://localhost:15672

Login: guest / guest

### 2. Set up the Database
Make sure you have MariaDB running locally.
You should already have a database and a hornethashes table with columns:

id (auto-increment)

date (datetime)

sha1 (varchar)

CREATE DATABASE hornettestdb;

USE hornettestdb;

CREATE TABLE hornethashes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    date DATE NOT NULL,
    sha1 VARCHAR(255) NOT NULL
);

### 3. Run the Projects
➡️ Run MessageProcessingSystemProcessor (Worker)
➡️ Run MessageProcessingSystemAPI (REST API)

### 4. Test the API

Call GenerateHashes endpoint
    40,000 hashes sent into the queue.

Call GetHashCounts
    Returns JSON result:
        {
          "hashes": [
            { "date": "2024-04-27", "count": 40000 }
           ]
        }

