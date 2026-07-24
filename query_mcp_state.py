import sqlite3
db = sqlite3.connect(r'C:\Users\33861\AppData\Roaming\Code\User\globalStorage\github.copilot-chat\session-store.db')
c = db.cursor()
c.execute("SELECT name FROM sqlite_master WHERE type='table'")
for row in c.fetchall():
    print(row[0])
db.close()
