const taskForm = document.getElementById("task-form");
const titleInput = document.getElementById("title");
const descriptionInput = document.getElementById("description");
const formError = document.getElementById("form-error");
const taskList = document.getElementById("task-list");
const emptyState = document.getElementById("empty-state");
const refreshBtn = document.getElementById("refresh-btn");
const template = document.getElementById("task-item-template");

async function fetchTasks() {
  const res = await fetch("/api/tasks");
  if (!res.ok) {
    throw new Error("Could not load tasks");
  }
  return res.json();
}

async function createTask(payload) {
  const res = await fetch("/api/tasks", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error || "Could not create task");
  }
}

async function updateTask(id, payload) {
  const res = await fetch(`/api/tasks/${id}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error("Could not update task");
  }
}

async function deleteTask(id) {
  const res = await fetch(`/api/tasks/${id}`, { method: "DELETE" });
  if (!res.ok) {
    throw new Error("Could not delete task");
  }
}

function renderTasks(tasks) {
  taskList.innerHTML = "";
  emptyState.hidden = tasks.length > 0;

  for (const task of tasks) {
    const node = template.content.cloneNode(true);
    const li = node.querySelector(".task-item");
    const checkbox = node.querySelector(".toggle");
    const title = node.querySelector(".task-title");
    const description = node.querySelector(".task-description");
    const deleteBtn = node.querySelector(".delete-btn");

    title.textContent = task.title;
    description.textContent = task.description || "No description";
    checkbox.checked = Boolean(task.done);
    li.classList.toggle("done", Boolean(task.done));

    checkbox.addEventListener("change", async () => {
      try {
        await updateTask(task.id, { done: checkbox.checked });
        await refresh();
      } catch (error) {
        checkbox.checked = !checkbox.checked;
        formError.textContent = error.message;
      }
    });

    deleteBtn.addEventListener("click", async () => {
      try {
        await deleteTask(task.id);
        await refresh();
      } catch (error) {
        formError.textContent = error.message;
      }
    });

    taskList.appendChild(node);
  }
}

async function refresh() {
  try {
    formError.textContent = "";
    const tasks = await fetchTasks();
    renderTasks(tasks);
  } catch (error) {
    formError.textContent = error.message;
  }
}

taskForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  try {
    formError.textContent = "";
    await createTask({
      title: titleInput.value,
      description: descriptionInput.value,
    });
    taskForm.reset();
    await refresh();
  } catch (error) {
    formError.textContent = error.message;
  }
});

refreshBtn.addEventListener("click", refresh);

refresh();
