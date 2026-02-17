This project is a distributed key-value store built to deeply understand consistent hashing, replica placement, and fault tolerance in distributed systems.

It simulates a multi-node cluster using ASP.NET Core APIs and Docker containers.

The system uses a custom consistent hash ring with virtual nodes to route requests across storage nodes and supports replication factor = 2 for fault tolerance.

🏗 Architecture
Client → Coordinator → Consistent Hash Ring → Node A / Node B / Node C

Components
🔹 Coordinator API

Maintains the Consistent Hash Ring

Uses SHA256-based hashing

Implements virtual nodes (100 per physical node)

Selects distinct physical replicas

Forwards HTTP requests to storage nodes

Implements read fallback on failure

🔹 Storage Nodes (A, B, C)

Independent ASP.NET Core APIs

Local in-memory key-value storage

Expose:

POST /put

GET /get/{key}

GET /health

🔹 Docker Cluster

Each node runs in a separate container

Coordinator communicates using container service names

Failure simulation via stopping containers

🔑 Key Concepts Implemented
✅ Consistent Hashing

Avoids hash(key) % N problem

Minimizes key movement during scaling

Clockwise traversal for ownership

✅ Virtual Nodes

100 virtual nodes per physical node

Improves load distribution

Reduces imbalance

✅ Replication (RF = 2)

Primary = first clockwise node

Replica = next distinct physical node

Ensures data durability

✅ Read Fallback

If primary node is down,

Coordinator attempts read from replica

Prevents data unavailability

🔁 Write Flow

Client sends PUT request.

Coordinator hashes the key.

Primary and replica nodes are selected.

Write is sent to both nodes.

🔎 Read Flow

Coordinator selects replica set.

Attempts read from primary.

If primary fails → tries replica.

Returns first successful response.

💥 Failure Simulation

Stopping one node container:

docker stop node-b


System continues serving reads if replica is alive.
