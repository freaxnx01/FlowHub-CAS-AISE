#uml

## Aggregation - Komposition

Der Hauptunterschied liegt in der Lebensdauer und der Abhängigkeit der Objekte.

### Aggregation (leere Raute)

Hierbei handelt es sich um eine lose „Teil-Ganzes“-Beziehung. Das Teilobjekt kann unabhängig vom Ganzen existieren.
 * Beispiel: Ein Fußballverein und seine Spieler. Wenn der Verein aufgelöst wird, existieren die Spieler weiterhin.
 * Symbol: \diamond

### Komposition (gefüllte Raute)

Dies ist eine starke Existenzabhängigkeit. Das Teil ist existentiell an das Ganze gebunden und wird mit ihm zerstört.
 * Beispiel: Ein Haus und seine Räume. Wird das Haus abgerissen, verschwinden auch die Räume.
 * Symbol: \blacklozenge
Soll ich dir ein konkretes Code-Beispiel zeigen, um den Unterschied in der Implementierung zu verdeutlichen?


## PlantUML

#plantuml

### Class diagram

```
class.puml
```

```
@startuml Domain Model

skinparam Style strictuml
skinparam SequenceMessageAlignment center
hide empty members

class Document {
  +int Id
  +string Title
  +DateTime CreatedDate
  +string CreatedBy
  +byte[] Content
  --
  +void Save()
  +void Delete()
  +Document GetById(int id)
}

class DocumentRepository {
  -IDbConnection connection
  --
  +Document FindById(int id)
  +void Insert(Document doc)
  +void Update(Document doc)
  +void Delete(int id)
}

class DocumentService {
  -DocumentRepository repo
  --
  +Document CreateDocument(DocumentDto dto)
  +void UpdateDocument(int id, DocumentDto dto)
  +void DeleteDocument(int id)
}

DocumentService --> DocumentRepository
DocumentRepository --> Document

@enduml
```


```
![Domain Model](https://www.plantuml.com/plantuml/svg/TP1DJiD038NtSmelMvLS0PKs0cNH3Iqhn68wyrH6P3nXngaK8Ux2MJWPoa-ZjgGx-_dP_knb4XsbzYNbmMl7j0aMfLBfmt6be_QqbQO49OdEjBxp2ZvhiC46AUa37aftO0yMCc11LE_EWk0hQSZ3xn6JKgRKAL4UJDqvlzJl3oqS2nMs2zizVA2TanATa6l1pddGEa8BR9kVEmTvrNJYlX6ylj4wi82bbR9C4IsEmLdQwYDcyyxNvJbAo4aPaPuWgwQminRAsRdwleP-HXMIan2R7ZyhylqwCCE82qnc38Vr5zCV7TkByIDDmGbHPgFJ1dDMVQci5jog3fVSHDuY7ft1m3jnI4GLfd3xxvxISNqkWQo4VtoZiIUveziDFV9LGuT-3PrbznE_LnDNd3kLMe9jxSi_ "Domain Model")
```

![Domain Model](https://www.plantuml.com/plantuml/svg/TP1DJiD038NtSmelMvLS0PKs0cNH3Iqhn68wyrH6P3nXngaK8Ux2MJWPoa-ZjgGx-_dP_knb4XsbzYNbmMl7j0aMfLBfmt6be_QqbQO49OdEjBxp2ZvhiC46AUa37aftO0yMCc11LE_EWk0hQSZ3xn6JKgRKAL4UJDqvlzJl3oqS2nMs2zizVA2TanATa6l1pddGEa8BR9kVEmTvrNJYlX6ylj4wi82bbR9C4IsEmLdQwYDcyyxNvJbAo4aPaPuWgwQminRAsRdwleP-HXMIan2R7ZyhylqwCCE82qnc38Vr5zCV7TkByIDDmGbHPgFJ1dDMVQci5jog3fVSHDuY7ft1m3jnI4GLfd3xxvxISNqkWQo4VtoZiIUveziDFV9LGuT-3PrbznE_LnDNd3kLMe9jxSi_ "Domain Model")

### Sequence diagram

```
sequence.puml
```

```
@startuml Hello World
skinparam Style strictuml
skinparam SequenceMessageAlignment center
A -> B: Hello World
A <- B: Hi
@enduml
```


```
![Hello World](https://www.plantuml.com/plantuml/svg/LOmn3eCm34NtdC8Nu0AeGeIfIwSEp57uGX6EAROpSFkAJcLzx-cDvaczLQOdc7UQT-Kbs5RaapHLUll987Cj-Qh-3Ou6oNZ1BAs8N5Qf4ASCSMY8r0qqzRTlf4VtOoMCaALL_W80 "Hello World")
```


![Hello World](https://www.plantuml.com/plantuml/svg/LOmn3eCm34NtdC8Nu0AeGeIfIwSEp57uGX6EAROpSFkAJcLzx-cDvaczLQOdc7UQT-Kbs5RaapHLUll987Cj-Qh-3Ou6oNZ1BAs8N5Qf4ASCSMY8r0qqzRTlf4VtOoMCaALL_W80 "Hello World")


