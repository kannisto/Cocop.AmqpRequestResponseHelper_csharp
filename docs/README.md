
Cocop.AmqpRequestResponseHelper (C#) v.2.0.0
============================================

---

<img src="logos.png" alt="COCOP and EU" style="display:block;margin-right:auto" />

COCOP - Coordinating Optimisation of Complex Industrial Processes  
https://cocop-spire.eu/

This project has received funding from the European Union's Horizon 2020
research and innovation programme under grant agreement No 723661. This piece
of software reflects only the authors' views, and the Commission is not
responsible for any use that may be made of the information contained therein.

---


Author
------

Petri Kannisto, Tampere University, Finland  
https://github.com/kannisto  
http://kannisto.org

**Please make sure to read and understand [LICENSE.txt](./LICENSE.txt)!**


COCOP Toolkit
-------------

This application is a part of COCOP Toolkit, which was developed to enable a
decoupled and well-scalable architecture in industrial systems. Please see
https://kannisto.github.io/Cocop-Toolkit/


Introduction
------------

This application is a software library to facilitate synchronous
request-response communication over the AMQP protocol (in particular,
RabbitMQ), because the native AMQP communication pattern is publish-subscribe
instead. This library provides you both a client and server.

This repository contains the following applications:

* Cocop.AmqpRequestResponseHelper API (DLL)
* Example: console application to provide a code example and perform
experiments (or tests) with the client and server

See also:

* Github repo: https://github.com/kannisto/Cocop.AmqpRequestResponseHelper_csharp
* API documentation: https://kannisto.github.io/Cocop.AmqpRequestResponseHelper_csharp


Environment and Libraries
-------------------------

The development environment was _Microsoft Visual Studio 2017 (Version 15.8.2)_.

The .NET Framework version is the ancient 4.5.1. This was chosen to reduce the
risk that a deployment environment does not have a version new enough.

The library was developed with _RabbitMQ.Client_ version 4.1.3. This library is
excluded from the repository. To install, type in Nuget:

```Install-Package RabbitMQ.Client -Version 4.1.3```
