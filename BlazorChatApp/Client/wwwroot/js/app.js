// Sample user data
const users = [
  { id: 'user1', pw: 'pass1', name: 'User One' },
  { id: 'user2', pw: 'pass2', name: 'User Two' },
  { id: 'user3', pw: 'pass3', name: 'User Three' },
  { id: 'user4', pw: 'pass4', name: 'User Four' },
  { id: 'user5', pw: 'pass5', name: 'User Five' }
];

// Save users to local storage
localStorage.setItem('users', JSON.stringify(users));

// Login function
function login(event) {
  event.preventDefault();
  const userId = document.getElementById('userId').value;
  const password = document.getElementById('password').value;
  const storedUsers = JSON.parse(localStorage.getItem('users'));

  const user = storedUsers.find(user => user.id === userId && user.pw === password);

  if (user) {
    window.location.href = 'main.html';
  } else {
    window.location.href = 'error.html';
  }
}

// Add event listener to login form
document.getElementById('loginForm').addEventListener('submit', login);
