# Innago.Shared.HeapService | v1

> Version 1.0.0

## Path Table

| Method | Path | Description |
| --- | --- | --- |
| POST | [/track](#posttrack) |  |

## Reference Table

| Name | Path | Description |
| --- | --- | --- |
| TrackEventParameters | [#/components/schemas/TrackEventParameters](#componentsschemastrackeventparameters) |  |

## Path Details

***

### [POST]/track

#### RequestBody

- application/json

```ts
{
  emailAddress: string
  eventName: string
  timestamp: string
  additionalProperties: {
  }
}
```

#### Responses

- 200 OK

## References

### #/components/schemas/TrackEventParameters

```ts
{
  emailAddress: string
  eventName: string
  timestamp: string
  additionalProperties: {
  }
}
```