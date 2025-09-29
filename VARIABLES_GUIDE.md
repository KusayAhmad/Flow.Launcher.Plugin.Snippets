# Interactive Variables Support in Snippets

## New Feature

Interactive variable support has been added to saved snippets. You can now create customizable templates using variables that can have their values passed during invocation.

## How to Use

### Creating a Snippet with Variables

When creating a new snippet, you can use variables enclosed in curly braces `{variable_name}`:

```
Key: pod down
Value: kubectl scale deploy {app} -n {namespace} --replicas={replicas}
```

### Invoking the Snippet with Variable Values

```
pod down app=dragon namespace=pxcs replicas=0
```

This will result in:
```
kubectl scale deploy dragon -n pxcs --replicas=0
```

## Practical Examples

### Example 1: Starting/Stopping Applications
```
Key: app scale
Value: kubectl scale deploy {app} -n {namespace} --replicas={count}

Usage: app scale app=frontend namespace=production count=3
Result: kubectl scale deploy frontend -n production --replicas=3
```

### Example 2: Database Connection
```
Key: db connect
Value: mysql -h {host} -u {user} -p{password} {database}

Usage: db connect host=localhost user=admin password=secret database=myapp
Result: mysql -h localhost -u admin -psecret myapp
```

### Example 3: File Copying
```
Key: copy file
Value: cp {source} {destination}

Usage: copy file source=/home/user/file.txt destination=/backup/
Result: cp /home/user/file.txt /backup/
```

## Rules and Features

### Variable Naming
- Must start with a letter or underscore
- Can contain letters, numbers, and underscores
- Case-sensitive

### Valid Examples:
- `{app}`
- `{namespace}`
- `{replica_count}`
- `{database_host}`

### Invalid Examples:
- `{1app}` (starts with a number)
- `{app-name}` (contains a dash)
- `{app name}` (contains a space)

### Automatic Help

If you don't pass all required variables, a help message will appear indicating the missing variables:

```
pod down app=dragon
```

Will show: `pod down (requires variables): Required variables: namespace, replicas. Example: pod down app=dragon namespace=value replicas=value`

### Backward Compatibility

Snippets that don't contain variables will work the same way as before.

## Technical Notes

- Variables are parsed using Regular Expression
- The system supports unlimited number of variables in the same snippet
- Undefined variables will remain as-is in the final text
- The system is safe and does not allow execution of malicious code